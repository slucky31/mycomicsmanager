using Application.Interfaces;

namespace Application.UnitTests.ComicInfoSearch;

public sealed class ComicArchiveResultTests
{
    [Fact]
    public void Constructor_Should_InitializeAllProperties()
    {
        var result = new ComicArchiveResult("/output/book.cbz", 12_345L, 48);

        result.ArchivePath.Should().Be("/output/book.cbz");
        result.FileSize.Should().Be(12_345L);
        result.PageCount.Should().Be(48);
    }

    [Fact]
    public void Equality_Should_BeTrue_WhenAllPropertiesMatch()
    {
        var a = new ComicArchiveResult("/output/book.cbz", 12_345L, 48);
        var b = new ComicArchiveResult("/output/book.cbz", 12_345L, 48);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_Should_BeFalse_WhenArchivePathDiffers()
    {
        var a = new ComicArchiveResult("/output/a.cbz", 1024L, 10);
        var b = new ComicArchiveResult("/output/b.cbz", 1024L, 10);

        a.Should().NotBe(b);
    }

    [Fact]
    public void WithExpression_Should_CreateNewInstanceWithModifiedProperty()
    {
        var original = new ComicArchiveResult("/output/book.cbz", 1024L, 10);
        var modified = original with { FileSize = 2048L };

        modified.FileSize.Should().Be(2048L);
        modified.ArchivePath.Should().Be("/output/book.cbz");
        modified.PageCount.Should().Be(10);
        modified.Should().NotBeSameAs(original);
    }
}
