using Domain.Errors;
using Persistence.LocalStorage;

namespace Persistence.Tests.LocalStorage;

public sealed class LegacyLibrarySourceTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("legacy-library-source-tests-").FullName;

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    private static LegacyLibrarySource CreateSut(string directory) => new(directory);

    [Fact]
    public void GetFirstFiles_Should_ReturnSourceNotFound_WhenTheFolderDoesNotExist()
    {
        var result = CreateSut(Path.Combine(_root, "missing")).GetFirstFiles(20);

        result.Error.Should().Be(LegacyMigrationError.SourceNotFound);
    }

    [Fact]
    public void GetFirstFiles_Should_ReturnTheFirstTopLevelFilesInNameOrder_AndTheirTotal()
    {
        foreach (var name in new[] { "c.cbz", "a.cbz", "B.cbz" })
        {
            File.WriteAllText(Path.Combine(_root, name), "x");
        }

        Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllText(Path.Combine(_root, "sub", "0.cbz"), "x");

        var result = CreateSut(_root).GetFirstFiles(2);

        result.Value!.Files.Select(Path.GetFileName).Should().Equal("B.cbz", "a.cbz");
        result.Value.TotalCount.Should().Be(3);
    }
}
