using Application.Books.IsbnScan;
using Application.Interfaces;
using Domain.Books;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Options;
using NSubstitute;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class IsbnPageScannerTests : IDisposable
{
    private const string Isbn = "9782800112343";
    private const string ArchivePath = "/data/A/book.cbz";

    private readonly string _pagesDir;
    private readonly IPageTextRecognizer _recognizer = Substitute.For<IPageTextRecognizer>();
    private readonly IComicPageReader _pageReader = Substitute.For<IComicPageReader>();
    private readonly IsbnOcrSettings _settings = new();

    public IsbnPageScannerTests()
    {
        _pagesDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_pagesDir);
        // Each page image is the index of the page, so the recognizer can tell which page it reads.
        _recognizer.RecognizeAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("Chapitre 1"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_pagesDir))
        {
            Directory.Delete(_pagesDir, true);
        }
        GC.SuppressFinalize(this);
    }

    private IsbnPageScanner CreateScanner() =>
        new(_recognizer, _pageReader, Options.Create(_settings), NullLogger<IsbnPageScanner>.Instance);

    private List<string> CreatePages(int count) =>
        [.. Enumerable.Range(0, count).Select(index =>
        {
            var path = Path.Combine(_pagesDir, $"page-{index + 1:000}.webp");
            File.WriteAllBytes(path, [(byte)index]);
            return path;
        })];

    private void PrintIsbnOnPage(int pageIndex, string text = $"ISBN {Isbn}") =>
        _recognizer.RecognizeAsync(Arg.Is<ReadOnlyMemory<byte>>(image => PageIndexOf(image) == pageIndex), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success(text));

    private static int PageIndexOf(ReadOnlyMemory<byte> image) => image.Span[0];

    private List<int> ReadPages() =>
        [.. _recognizer.ReceivedCalls().Select(call => PageIndexOf((ReadOnlyMemory<byte>)call.GetArguments()[0]!))];

    [Fact]
    public async Task ScanPagesAsync_Should_StopOnTheFirstPageWithAnIsbn_ReadingTheMostLikelyPagesFirst()
    {
        var pages = CreatePages(20);
        PrintIsbnOnPage(18);

        var result = await CreateScanner().ScanPagesAsync(pages, TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new IsbnScanResult(true, [Isbn]));
        ReadPages().Should().Equal(1, 2, 19, 18);
    }

    [Fact]
    public async Task ScanPagesAsync_Should_ReturnEveryIsbnOfThePage_WhenItHasSeveral()
    {
        var pages = CreatePages(20);
        PrintIsbnOnPage(2, $"ISBN {Isbn}\nIntégrale : 2-205-05617-4");

        var result = await CreateScanner().ScanPagesAsync(pages, TestContext.Current.CancellationToken);

        result.Isbns.Should().Equal(Isbn, "2205056174");
    }

    [Fact]
    public async Task ScanPagesAsync_Should_CompleteWithoutIsbn_WhenNoScannedPageHasOne()
    {
        var pages = CreatePages(20);

        var result = await CreateScanner().ScanPagesAsync(pages, TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new IsbnScanResult(true, []));
        ReadPages().Should().HaveCount(10);
    }

    [Fact]
    public async Task ScanPagesAsync_Should_SkipAPage_WhenItCannotBeReadOrRecognized()
    {
        var pages = CreatePages(20);
        File.Delete(pages[1]);
        _recognizer.RecognizeAsync(Arg.Is<ReadOnlyMemory<byte>>(image => PageIndexOf(image) == 2), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(OcrError.Timeout));
        PrintIsbnOnPage(19);

        var result = await CreateScanner().ScanPagesAsync(pages, TestContext.Current.CancellationToken);

        result.Isbns.Should().Equal(Isbn);
        ReadPages().Should().Equal(2, 19);
    }

    [Fact]
    public async Task ScanPagesAsync_Should_ReturnNotScanned_WhenTheOcrEngineIsUnavailable()
    {
        var pages = CreatePages(20);
        _recognizer.RecognizeAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure(OcrError.Unavailable));

        var result = await CreateScanner().ScanPagesAsync(pages, TestContext.Current.CancellationToken);

        result.Should().Be(IsbnScanResult.NotScanned);
        ReadPages().Should().ContainSingle();
    }

    [Fact]
    public async Task ScanPagesAsync_Should_ReturnNotScanned_WhenTheOcrIsDisabled()
    {
        _settings.Enabled = false;

        var result = await CreateScanner().ScanPagesAsync(CreatePages(20), TestContext.Current.CancellationToken);

        result.Should().Be(IsbnScanResult.NotScanned);
        _recognizer.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task ScanArchiveAsync_Should_ReadThePagesFromTheArchive()
    {
        _pageReader.CountPagesAsync(ArchivePath, Arg.Any<CancellationToken>()).Returns(Result<int>.Success(20));
        // Page 2 (index 1) cannot be read: the scan goes on with the next page.
        _pageReader.ReadPageAsync(ArchivePath, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<int>(1) == 1
                ? Result<ComicPage>.Failure(BooksError.PageNotFound)
                : Result<ComicPage>.Success(new ComicPage(new[] { (byte)call.ArgAt<int>(1) }, "image/webp", DateTime.UtcNow)));
        PrintIsbnOnPage(2);

        var result = await CreateScanner().ScanArchiveAsync(ArchivePath, TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new IsbnScanResult(true, [Isbn]));
        ReadPages().Should().Equal(2);
    }

    [Fact]
    public async Task ScanArchiveAsync_Should_ReturnNotScanned_WhenTheArchiveCannotBeRead()
    {
        _pageReader.CountPagesAsync(ArchivePath, Arg.Any<CancellationToken>()).Returns(Result<int>.Failure(FileProcessingError.CorruptArchive));

        var result = await CreateScanner().ScanArchiveAsync(ArchivePath, TestContext.Current.CancellationToken);

        result.Should().Be(IsbnScanResult.NotScanned);
    }

    [Fact]
    public async Task ScanArchiveAsync_Should_ReturnNotScanned_WhenTheOcrIsDisabled()
    {
        _settings.Enabled = false;

        var result = await CreateScanner().ScanArchiveAsync(ArchivePath, TestContext.Current.CancellationToken);

        result.Should().Be(IsbnScanResult.NotScanned);
        _pageReader.ReceivedCalls().Should().BeEmpty();
    }
}
