using System.IO.Compression;
using System.Xml.Linq;
using Application.Interfaces;
using Domain.Books;
using Domain.Errors;
using Persistence.Services;

namespace Application.UnitTests.Services;

public sealed class ComicInfoArchiveWriterTests : IDisposable
{
    private readonly string _rootDir;
    private readonly ComicInfoArchiveWriter _writer;

    private static readonly ComicInfoData s_data = new(
        Title: "Quelque part entre les ombres", Series: "Blacksad", Number: 1, Summary: null,
        Year: 2000, Month: 11, Day: 1, Writer: null, Penciller: null, Publisher: "Dargaud",
        Isbn: "9782205050196", PageCount: null);

    public ComicInfoArchiveWriterTests()
    {
        _rootDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_rootDir);
        _writer = new ComicInfoArchiveWriter(_rootDir, NullLogger<ComicInfoArchiveWriter>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDir))
        {
            Directory.Delete(_rootDir, true);
        }
        GC.SuppressFinalize(this);
    }

    private string CreateCbz(string? comicInfo, string fileName = "book.cbz")
    {
        var path = Path.Combine(_rootDir, fileName);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var page in new[] { "page-001.webp", "page-002.webp" })
        {
            using var stream = archive.CreateEntry(page).Open();
            stream.Write([0x52, 0x49, 0x46, 0x46]);
        }

        if (comicInfo is not null)
        {
            using var writer = new StreamWriter(archive.CreateEntry("ComicInfo.xml").Open());
            writer.Write(comicInfo);
        }

        return path;
    }

    private static XElement ReadComicInfo(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        archive.Entries.Count(e => e.Name == "ComicInfo.xml").Should().Be(1);
        using var stream = archive.GetEntry("ComicInfo.xml")!.Open();
        return XDocument.Load(stream).Root!;
    }

    [Fact]
    public async Task WriteAsync_Should_UpdateTheBookElements_AndKeepTheOthers()
    {
        var path = CreateCbz("""
            <ComicInfo>
              <Title>Titre</Title>
              <Series>Blacksad</Series>
              <Number>1</Number>
              <Summary>Un chat noir...</Summary>
              <Writer>Juan Diaz Canales</Writer>
              <Penciller>Juanjo Guarnido</Penciller>
              <Genre>Polar</Genre>
              <PageCount>48</PageCount>
            </ComicInfo>
            """);

        var result = await _writer.WriteAsync(path, s_data, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var root = ReadComicInfo(path);
        root.Element("Title")!.Value.Should().Be("Quelque part entre les ombres");
        root.Element("Number")!.Value.Should().Be("1");
        root.Element("Volume")!.Value.Should().Be("1");
        root.Element("Year")!.Value.Should().Be("2000");
        root.Element("Publisher")!.Value.Should().Be("Dargaud");
        root.Element("ISBN")!.Value.Should().Be("9782205050196");
        root.Element("Writer").Should().BeNull();
        root.Element("Summary")!.Value.Should().Be("Un chat noir...");
        root.Element("Penciller")!.Value.Should().Be("Juanjo Guarnido");
        root.Element("Genre")!.Value.Should().Be("Polar");
        root.Element("PageCount")!.Value.Should().Be("48");
        await using var archive = await ZipFile.OpenReadAsync(path, TestContext.Current.CancellationToken);
        archive.Entries.Select(e => e.FullName).Should().Contain(["page-001.webp", "page-002.webp"]);
        Directory.GetFiles(_rootDir).Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("<ComicInfo><Title>")]
    [InlineData("<Other><Genre>Polar</Genre></Other>")]
    public async Task WriteAsync_Should_WriteANewComicInfo_WhenTheArchiveHasNoUsableOne(string? comicInfo)
    {
        var path = CreateCbz(comicInfo);

        var result = await _writer.WriteAsync(path, s_data with { Summary = "Résumé", Penciller = "Guarnido", PageCount = 2 },
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var root = ReadComicInfo(path);
        root.Name.LocalName.Should().Be("ComicInfo");
        root.Element("Series")!.Value.Should().Be("Blacksad");
        root.Element("Summary")!.Value.Should().Be("Résumé");
        root.Element("Penciller")!.Value.Should().Be("Guarnido");
        root.Element("PageCount")!.Value.Should().Be("2");
        root.Element("Genre").Should().BeNull();
    }

    [Fact]
    public async Task WriteAsync_Should_RefuseTheArchive_WhenItIsOutsideTheRoot()
    {
        var result = await _writer.WriteAsync(Path.Combine(Path.GetTempPath(), "elsewhere.cbz"), s_data,
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(FileProcessingError.InvalidPath);
    }

    [Fact]
    public async Task WriteAsync_Should_ReturnFileNotFound_WhenTheArchiveDoesNotExist()
    {
        var result = await _writer.WriteAsync(Path.Combine(_rootDir, "missing.cbz"), s_data,
            TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.FileNotFound);
    }

    [Fact]
    public async Task WriteAsync_Should_LeaveTheFileUntouched_WhenItIsNotACbz()
    {
        var path = Path.Combine(_rootDir, "book.cbr");
        await File.WriteAllBytesAsync(path, [1, 2, 3], TestContext.Current.CancellationToken);

        var result = await _writer.WriteAsync(path, s_data, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task WriteAsync_Should_FailAndKeepTheFile_WhenTheArchiveIsCorrupt()
    {
        var path = Path.Combine(_rootDir, "book.cbz");
        await File.WriteAllBytesAsync(path, [1, 2, 3], TestContext.Current.CancellationToken);

        var result = await _writer.WriteAsync(path, s_data, TestContext.Current.CancellationToken);

        result.Error.Should().Be(FileProcessingError.XmlWriteError);
        (await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).Should().Equal(1, 2, 3);
        Directory.GetFiles(_rootDir).Should().ContainSingle();
    }

    [Fact]
    public async Task WriteAsync_Should_RemoveTheTemporaryFile_WhenCancelled()
    {
        var path = CreateCbz(null);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => _writer.WriteAsync(path, s_data, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        Directory.GetFiles(_rootDir).Should().ContainSingle();
    }
}
