using System.Net;
using Application.ImportJobs;
using AwesomeAssertions;
using Domain.FeedImports;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Web.Infrastructure;
using Xunit;

namespace Web.Tests.Infrastructure;

public sealed class FeedImportFileDownloaderTests : IDisposable
{
    private static readonly Uri s_downloadUrl = new("https://srv1.debrid.link/dl/x1/Blacksad.cbz");

    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"mcm-feed-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private async Task<Domain.Primitives.Result<Application.FeedImports.DownloadedFile>> DownloadAsync(
        Func<HttpResponseMessage> responder, long maxBytes = 1_000)
    {
        using var handler = new StubHandler(responder);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        var downloader = new FeedImportFileDownloader(httpClient, Options.Create(new ImportSettings { TempDirectory = _tempDirectory }), NullLogger<FeedImportFileDownloader>.Instance);
        return await downloader.DownloadAsync(s_downloadUrl, maxBytes, TestContext.Current.CancellationToken);
    }

    private string[] DownloadedFiles() =>
        Directory.Exists(_tempDirectory) ? Directory.GetFiles(_tempDirectory, "*", SearchOption.AllDirectories) : [];

    [Fact]
    public async Task DownloadAsync_Should_WriteFileIntoTempDirectory_WhenServed()
    {
        var result = await DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[600]) });

        result.IsSuccess.Should().BeTrue();
        result.Value!.SizeBytes.Should().Be(600);
        result.Value.TempFilePath.Should().StartWith(_tempDirectory);
        new FileInfo(result.Value.TempFilePath).Length.Should().Be(600);
    }

    [Fact]
    public async Task DownloadAsync_Should_FailAndDeletePartialFile_WhenStreamExceedsLimitWithoutContentLength()
    {
        var result = await DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream(new byte[1_001]))
        });

        result.Error.Should().Be(FeedImportError.FileTooLarge);
        DownloadedFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_Should_ReturnDownloadFailed_WhenStatusIsNotSuccess()
    {
        var result = await DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.NotFound));

        result.Error!.Code.Should().Be(FeedImportError.DownloadFailed.Code);
        result.Error.Description.Should().Contain("404");
        DownloadedFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_Should_ReturnDownloadFailed_WhenTransportFails()
    {
        var result = await DownloadAsync(() => throw new HttpRequestException("reset"));

        result.Error.Should().Be(FeedImportError.DownloadFailed);
        DownloadedFiles().Should().BeEmpty();
    }

    private sealed class StubHandler(Func<HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder());
    }
}
