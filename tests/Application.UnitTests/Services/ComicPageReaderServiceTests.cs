using System.IO.Compression;
using System.Text;
using Domain.Books;
using Domain.Errors;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class ComicPageReaderServiceTests : IDisposable
{
    private readonly string _rootDir;
    private readonly ComicPageReaderService _service;

    public ComicPageReaderServiceTests()
    {
        _rootDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_rootDir);
        _service = new ComicPageReaderService(_rootDir, NullLogger<ComicPageReaderService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDir))
        {
            Directory.Delete(_rootDir, true);
        }
        GC.SuppressFinalize(this);
    }

    // Each entry content is its own name, so a test can tell which page was returned.
    private string CreateArchive(params string[] entryNames)
    {
        var path = Path.Combine(_rootDir, "book.cbz");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var name in entryNames)
        {
            using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
            writer.Write(name);
        }
        return path;
    }

    [Fact]
    public async Task CountPagesAsync_Should_CountImagesOnly()
    {
        var path = CreateArchive("page-1.webp", "page-2.jpg", "ComicInfo.xml", "notes.txt");

        var result = await _service.CountPagesAsync(path, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task ReadPageAsync_Should_ReturnPagesInNaturalOrder()
    {
        var path = CreateArchive("page10.webp", "page2.webp", "Page1.webp");
        var ct = TestContext.Current.CancellationToken;

        var pages = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var result = await _service.ReadPageAsync(path, i, ct);
            result.IsSuccess.Should().BeTrue();
            pages.Add(Encoding.UTF8.GetString(result.Value!.Content.Span));
        }

        pages.Should().Equal("Page1.webp", "page2.webp", "page10.webp");
    }

    [Fact]
    public async Task ReadPageAsync_Should_ReturnContentTypeAndArchiveDate()
    {
        var path = CreateArchive("cover.jpg");

        var result = await _service.ReadPageAsync(path, 0, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be("image/jpeg");
        result.Value.LastModifiedUtc.Should().Be(File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public async Task ReadPageAsync_Should_NotWriteAnyFile()
    {
        var path = CreateArchive("page-1.webp", "page-2.webp");

        var result = await _service.ReadPageAsync(path, 1, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        Directory.GetFileSystemEntries(_rootDir).Should().Equal(path);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public async Task ReadPageAsync_Should_ReturnPageNotFound_WhenIndexIsOutOfRange(int pageIndex)
    {
        var path = CreateArchive("page-1.webp", "page-2.webp");

        var result = await _service.ReadPageAsync(path, pageIndex, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.PageNotFound);
    }

    [Fact]
    public async Task ReadPageAsync_Should_ReturnFileNotFound_WhenArchiveIsMissing()
    {
        var result = await _service.ReadPageAsync(Path.Combine(_rootDir, "missing.cbz"), 0, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.FileNotFound);
    }

    [Fact]
    public async Task ReadPageAsync_Should_ReturnInvalidPath_WhenArchiveIsOutsideTheRoot()
    {
        var outsidePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cbz");

        var result = await _service.ReadPageAsync(outsidePath, 0, TestContext.Current.CancellationToken);

        result.Error.Should().Be(FileProcessingError.InvalidPath);
    }

    [Fact]
    public async Task CountPagesAsync_Should_ReturnCorruptArchive_WhenFileIsNotAnArchive()
    {
        var path = Path.Combine(_rootDir, "broken.cbz");
        await File.WriteAllTextAsync(path, "not a zip", TestContext.Current.CancellationToken);

        var result = await _service.CountPagesAsync(path, TestContext.Current.CancellationToken);

        result.Error.Should().Be(FileProcessingError.CorruptArchive);
    }
}
