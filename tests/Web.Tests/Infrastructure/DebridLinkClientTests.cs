using System.Net;
using System.Text;
using AwesomeAssertions;
using Domain.FeedImports;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class DebridLinkClientTests
{
    private static readonly Uri s_baseAddress = new("https://debrid-link.com/api/v2/");

    private static (DebridLinkClient Client, RecordingHandler Handler, HttpClient HttpClient) Build(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new RecordingHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = s_baseAddress };
        return (new DebridLinkClient(httpClient, NullLogger<DebridLinkClient>.Instance), handler, httpClient);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task CheckAccountAsync_Should_Succeed_WhenDebridLinkReturnsAccountInfos()
    {
        var (client, handler, httpClient) = Build(_ => Json("""{"success":true,"value":{"username":"reader","accountType":1}}"""));
        using var _ = httpClient;

        var result = await client.CheckAccountAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        handler.Requests.Should().ContainSingle().Which.Should().Be("GET /api/v2/account/infos");
    }

    [Fact]
    public async Task CheckAccountAsync_Should_ReturnUnauthorized_WhenTokenIsRejected()
    {
        var (client, _, httpClient) = Build(_ => Json("""{"success":false,"error":"badToken"}""", HttpStatusCode.Unauthorized));
        using var _ = httpClient;

        var result = await client.CheckAccountAsync(TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedImportError.DebridLinkUnauthorized);
    }

    [Fact]
    public async Task UnlockAsync_Should_PostUrlAndMapDownloadLink_WhenDebridLinkSucceeds()
    {
        var (client, handler, httpClient) = Build(_ => Json(
            """{"success":true,"value":{"id":"x1","name":"Blacksad T03.cbz","url":"https://1fichier.com/?abc","downloadUrl":"https://srv1.debrid.link/dl/x1/Blacksad%20T03.cbz","size":52428800,"created":1700000000}}"""));
        using var _ = httpClient;

        var result = await client.UnlockAsync("https://1fichier.com/?abc", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Blacksad T03.cbz");
        result.Value.SizeBytes.Should().Be(52_428_800);
        result.Value.DownloadUrl.Host.Should().Be("srv1.debrid.link");
        handler.Requests.Should().ContainSingle().Which.Should().Be("POST /api/v2/downloader/add");
        handler.Bodies.Should().ContainSingle().Which.Should().Be("""{"url":"https://1fichier.com/?abc"}""");
    }

    [Fact]
    public async Task UnlockAsync_Should_UseFirstLink_WhenDebridLinkReturnsAFolder()
    {
        var (client, _, httpClient) = Build(_ => Json(
            """{"success":true,"value":[{"name":"a.cbz","downloadUrl":"https://srv1.debrid.link/dl/a"},{"name":"b.cbz","downloadUrl":"https://srv1.debrid.link/dl/b"}]}"""));
        using var _ = httpClient;

        var result = await client.UnlockAsync("https://1fichier.com/dir/?abc", TestContext.Current.CancellationToken);

        result.Value!.Name.Should().Be("a.cbz");
        result.Value.SizeBytes.Should().BeNull();
    }

    [Fact]
    public async Task UnlockAsync_Should_ReturnUnavailable_WhenDownloadUrlIsMissing()
    {
        var (client, _, httpClient) = Build(_ => Json("""{"success":true,"value":{"name":"a.cbz"}}"""));
        using var _ = httpClient;

        var result = await client.UnlockAsync("https://1fichier.com/?abc", TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.DebridLinkUnavailable);
    }

    [Fact]
    public async Task UnlockAsync_Should_MapApiErrorCode_WhenDebridLinkRefuses()
    {
        var (client, _, httpClient) = Build(_ => Json("""{"success":false,"error":"badToken"}""", HttpStatusCode.Unauthorized));
        using var _ = httpClient;

        var result = await client.UnlockAsync("https://1fichier.com/?abc", TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.DebridLinkUnauthorized);
    }

    [Fact]
    public async Task UnlockAsync_Should_ReturnUnavailable_WhenTransportFails()
    {
        var (client, _, httpClient) = Build(_ => throw new HttpRequestException("connection refused"));
        using var _ = httpClient;

        var result = await client.UnlockAsync("https://1fichier.com/?abc", TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.DebridLinkUnavailable);
    }

    [Fact]
    public async Task UnlockAsync_Should_ReturnUnavailable_WhenBodyIsNotJson()
    {
        var (client, _, httpClient) = Build(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>502</html>") });
        using var _ = httpClient;

        var result = await client.UnlockAsync("https://1fichier.com/?abc", TestContext.Current.CancellationToken);

        result.Error.Should().Be(FeedImportError.DebridLinkUnavailable);
    }

    [Fact]
    public async Task GetSupportedDomainsAsync_Should_ReturnDomains_WhenDebridLinkSucceeds()
    {
        var (client, handler, httpClient) = Build(_ => Json("""{"success":true,"value":["1fichier.com","rapidgator.net",""]}"""));
        using var _ = httpClient;

        var result = await client.GetSupportedDomainsAsync(TestContext.Current.CancellationToken);

        result.Value.Should().Equal("1fichier.com", "rapidgator.net");
        handler.Requests.Should().ContainSingle().Which.Should().Be("GET /api/v2/downloader/domains");
    }

    [Theory]
    [InlineData("maxLink", "FEED429D")]
    [InlineData("maxData", "FEED429D")]
    [InlineData("maxLinkHost", "FEED429H")]
    [InlineData("maxDataHost", "FEED429H")]
    [InlineData("fileNotFound", "FEED410D")]
    [InlineData("fileNotAvailable", "FEED410D")]
    [InlineData("hostNotValid", "FEED422D")]
    [InlineData("notDebrid", "FEED422D")]
    [InlineData("floodDetected", "FEED502D")]
    [InlineData(null, "FEED502D")]
    public void MapError_Should_TranslateDocumentedCodes(string? apiCode, string expectedCode)
    {
        DebridLinkClient.MapError(apiCode).Code.Should().Be(expectedCode);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
            if (request.Content is not null)
            {
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }
            return responder(request);
        }
    }
}
