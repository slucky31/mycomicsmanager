using System.Net;
using Application.FeedImports;
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

    private readonly FeedImportSettings _feedImportSettings = new();

    private FeedImportFileDownloader CreateDownloader(HttpClient httpClient) =>
        new(httpClient, Options.Create(new ImportSettings { TempDirectory = _tempDirectory }), Options.Create(_feedImportSettings), NullLogger<FeedImportFileDownloader>.Instance);

    private async Task<Domain.Primitives.Result<DownloadedFile>> DownloadAsync(
        Func<HttpResponseMessage> responder, long maxBytes = 1_000, CancellationToken? cancellationToken = null)
    {
        using var handler = new StubHandler(responder);
        using var httpClient = new HttpClient(handler, disposeHandler: false);
        return await CreateDownloader(httpClient).DownloadAsync(s_downloadUrl, maxBytes, cancellationToken ?? TestContext.Current.CancellationToken);
    }

    private string FeedImportsDirectory => Path.Combine(_tempDirectory, "feed-imports");

    private string GivenFile(string name, TimeSpan age)
    {
        Directory.CreateDirectory(FeedImportsDirectory);
        var path = Path.Combine(FeedImportsDirectory, name);
        File.WriteAllBytes(path, [1, 2, 3]);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow - age);
        return path;
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
            Content = new StreamContent(new ScriptedStream(chunks: 101, delayAfterChunks: TimeSpan.Zero))
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
    public async Task DownloadAsync_Should_ReturnDownloadFailed_WhenTheFileIsEmpty()
    {
        var result = await DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) });

        result.Error!.Code.Should().Be(FeedImportError.DownloadFailed.Code);
        result.Error.Description.Should().EndWith("(empty file)");
    }

    [Fact]
    public async Task DownloadAsync_Should_ReturnDownloadFailed_WhenTransportFails()
    {
        var result = await DownloadAsync(() => throw new HttpRequestException("reset"));

        result.Error.Should().Be(FeedImportError.DownloadFailed);
        DownloadedFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_Should_FailAndDeletePartialFile_WhenServerStopsSendingData()
    {
        _feedImportSettings.DownloadIdleTimeout = TimeSpan.FromMilliseconds(200);

        var result = await DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new ScriptedStream(chunks: 2, delayAfterChunks: Timeout.InfiniteTimeSpan))
        });

        result.Error!.Code.Should().Be(FeedImportError.DownloadFailed.Code);
        result.Error.Description.Should().EndWith("(no data received for 0.2 s)");
        DownloadedFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_Should_FailAndDeletePartialFile_WhenDownloadTakesLongerThanTimeout()
    {
        _feedImportSettings.DownloadTimeout = TimeSpan.FromMilliseconds(300);

        // Data keeps coming (the idle delay is never reached) but the file never ends.
        var result = await DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new ScriptedStream(chunks: int.MaxValue, delayAfterChunks: TimeSpan.Zero, delayPerChunk: TimeSpan.FromMilliseconds(20)))
        }, maxBytes: long.MaxValue);

        result.Error!.Code.Should().Be(FeedImportError.DownloadFailed.Code);
        result.Error.Description.Should().EndWith("(took longer than 0.3 s)");
        DownloadedFiles().Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_Should_ThrowAndDeletePartialFile_WhenCallerCancels()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        var act = () => DownloadAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new ScriptedStream(chunks: 1, delayAfterChunks: Timeout.InfiniteTimeSpan))
        }, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        DownloadedFiles().Should().BeEmpty();
    }

    [Fact]
    public void DiscardStale_Should_DeleteOnlyOldPartialDownloads_WhenCalled()
    {
        var stale = GivenFile("old.download", TimeSpan.FromHours(2));
        var running = GivenFile("running.download", TimeSpan.FromSeconds(5));
        var other = GivenFile("old.cbz", TimeSpan.FromHours(2));
        using var httpClient = new HttpClient();

        var deleted = CreateDownloader(httpClient).DiscardStale(TimeSpan.FromMinutes(30));

        deleted.Should().Be(1);
        File.Exists(stale).Should().BeFalse();
        File.Exists(running).Should().BeTrue();
        File.Exists(other).Should().BeTrue();
    }

    [Fact]
    public void DiscardStale_Should_ReturnZero_WhenDirectoryDoesNotExist()
    {
        using var httpClient = new HttpClient();

        CreateDownloader(httpClient).DiscardStale(TimeSpan.FromMinutes(30)).Should().Be(0);
    }

    [Theory]
    [InlineData(60, "1 min")]
    [InlineData(1200, "20 min")]
    [InlineData(90, "90 s")]
    [InlineData(30, "30 s")]
    public void Format_Should_UseMinutesOnlyForWholeMinutes_WhenFormattingADelay(int seconds, string expected)
    {
        FeedImportFileDownloader.Format(TimeSpan.FromSeconds(seconds)).Should().Be(expected);
    }

    // Serves `chunks` chunks of 10 bytes (delayPerChunk apart), then waits delayAfterChunks before each next read.
    private sealed class ScriptedStream(int chunks, TimeSpan delayAfterChunks, TimeSpan delayPerChunk = default) : Stream
    {
        private int _served;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_served >= chunks)
            {
                await Task.Delay(delayAfterChunks, cancellationToken);
                return 0;
            }

            if (delayPerChunk > TimeSpan.Zero)
            {
                await Task.Delay(delayPerChunk, cancellationToken);
            }

            _served++;
            var size = Math.Min(10, buffer.Length);
            buffer.Span[..size].Fill(1);
            return size;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class StubHandler(Func<HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder());
    }
}
