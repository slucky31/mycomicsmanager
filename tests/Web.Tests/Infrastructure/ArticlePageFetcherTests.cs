using System.Net;
using System.Text;
using AwesomeAssertions;
using Domain.FeedImports;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class ArticlePageFetcherTests
{
    private static readonly Uri s_page = new("https://planete-bd.org/blacksad-3");

    private static async Task<Domain.Primitives.Result<string>> FetchAsync(Func<HttpResponseMessage> responder)
    {
        using var handler = new StubHandler(responder);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        return await new ArticlePageFetcher(httpClient, NullLogger<ArticlePageFetcher>.Instance).GetHtmlAsync(s_page, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetHtmlAsync_Should_ReturnHtml_WhenPageIsServed()
    {
        var result = await FetchAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<p>Âme rouge</p>", Encoding.UTF8, "text/html")
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("<p>Âme rouge</p>");
    }

    [Fact]
    public async Task GetHtmlAsync_Should_DecodeDeclaredCharset()
    {
        var content = new ByteArrayContent(Encoding.Latin1.GetBytes("<p>Âme rouge</p>"));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html") { CharSet = "iso-8859-1" };

        var result = await FetchAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

        result.Value.Should().Be("<p>Âme rouge</p>");
    }

    [Fact]
    public async Task GetHtmlAsync_Should_ReturnPageUnavailable_WhenStatusIsNotSuccess()
    {
        var result = await FetchAsync(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(FeedImportError.PageUnavailable.Code);
        result.Error.Description.Should().Contain("404");
    }

    [Fact]
    public async Task GetHtmlAsync_Should_Fail_WhenPageIsTooLarge_EvenWithoutContentLength()
    {
        var result = await FetchAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(new byte[ArticlePageFetcher.MaxPageBytes + 1]))
        });

        result.IsFailure.Should().BeTrue();
        result.Error!.Description.Should().Contain("trop volumineuse");
    }

    [Fact]
    public async Task GetHtmlAsync_Should_ReturnPageUnavailable_WhenTransportFails()
    {
        var result = await FetchAsync(() => throw new HttpRequestException("SSRF guard: request not permitted."));

        result.Error.Should().Be(FeedImportError.PageUnavailable);
    }

    private sealed class StubHandler(Func<HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder());
    }
}
