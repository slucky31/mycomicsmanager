using System.Net;
using System.Text;
using AwesomeAssertions;
using Domain.FeedImports;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class MinifluxClientTests
{
    private static readonly Uri s_baseAddress = new("http://miniflux:8080/");

    private static (MinifluxClient Client, RoutingHandler Handler, HttpClient HttpClient) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new RoutingHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = s_baseAddress };
        return (new MinifluxClient(httpClient), handler, httpClient);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string EntryJson(long id, bool starred = true) =>
        $$"""{"id":{{id}},"title":"Entry {{id}}","url":"https://planete-bd.org/{{id}}","published_at":"2026-09-30T20:00:00+02:00","starred":{{(starred ? "true" : "false")}}}""";

    [Fact]
    public async Task GetCategoriesAsync_Should_MapCategories_WhenMinifluxResponds()
    {
        var (client, handler, httpClient) = Build(_ => Json("""[{"id":1,"title":"Tech"},{"id":7,"title":"BD"}]"""));
        using var _ = httpClient;

        var result = await client.GetCategoriesAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![1].Id.Should().Be(7);
        result.Value[1].Title.Should().Be("BD");
        handler.Requests.Should().ContainSingle().Which.Should().Be("GET /v1/categories");
    }

    [Fact]
    public async Task GetCategoriesAsync_Should_ReturnMinifluxUnavailable_WhenStatusIsNotSuccess()
    {
        var (client, _, httpClient) = Build(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var _ = httpClient;

        var result = await client.GetCategoriesAsync(TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.MinifluxUnavailable);
    }

    [Fact]
    public async Task GetCategoriesAsync_Should_ReturnMinifluxUnavailable_WhenTransportFails()
    {
        var (client, _, httpClient) = Build(_ => throw new HttpRequestException("connection refused"));
        using var _ = httpClient;

        var result = await client.GetCategoriesAsync(TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.MinifluxUnavailable);
    }

    [Fact]
    public async Task GetStarredEntriesAsync_Should_FollowPagination_WhenTotalExceedsPageSize()
    {
        var firstPage = string.Join(',', Enumerable.Range(1, 100).Select(i => EntryJson(i)));
        var (client, handler, httpClient) = Build(request =>
            request.RequestUri!.Query.Contains("offset=0", StringComparison.Ordinal)
                ? Json($$"""{"total":101,"entries":[{{firstPage}}]}""")
                : Json($$"""{"total":101,"entries":[{{EntryJson(101)}}]}"""));
        using var _ = httpClient;

        var result = await client.GetStarredEntriesAsync(7, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(101);
        result.Value![100].Id.Should().Be(101);
        result.Value[100].Url.Should().Be("https://planete-bd.org/101");
        result.Value[100].PublishedAt.Should().Be(new DateTimeOffset(2026, 9, 30, 18, 0, 0, TimeSpan.Zero));
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].Should().Be("GET /v1/entries?starred=true&category_id=7&order=published_at&direction=asc&limit=100&offset=0");
        handler.Requests[1].Should().EndWith("offset=100");
    }

    [Fact]
    public async Task UnstarAsync_Should_ToggleBookmark_WhenEntryIsStarred()
    {
        var (client, handler, httpClient) = Build(request =>
            request.Method == HttpMethod.Get ? Json(EntryJson(42)) : new HttpResponseMessage(HttpStatusCode.NoContent));
        using var _ = httpClient;

        var result = await client.UnstarAsync(42, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        handler.Requests.Should().Equal("GET /v1/entries/42", "PUT /v1/entries/42/bookmark");
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task UnstarAsync_Should_NotToggle_WhenEntryIsNoLongerStarred(HttpStatusCode status)
    {
        var (client, handler, httpClient) = Build(_ => Json(EntryJson(42, starred: false), status));
        using var _ = httpClient;

        var result = await client.UnstarAsync(42, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        handler.Requests.Should().Equal("GET /v1/entries/42");
    }

    [Fact]
    public async Task UnstarAsync_Should_ReturnMinifluxUnavailable_WhenToggleFails()
    {
        var (client, _, httpClient) = Build(request =>
            request.Method == HttpMethod.Get ? Json(EntryJson(42)) : new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var _ = httpClient;

        var result = await client.UnstarAsync(42, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.MinifluxUnavailable);
    }

    private sealed class RoutingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            return Task.FromResult(responder(request));
        }
    }
}
