
using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Libraries;

namespace Persistence.Tests;

[Collection("DatabaseCollectionTests")]
public class UnitOfWorkTests(IntegrationTestWebAppFactory factory) : LibraryIntegrationTest(factory)
{
    private static readonly Guid s_userId = Guid.CreateVersion7();

    [Fact]
    public async Task Savechanges_Create()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();
        var libName = "Create_" + guid;
        var lib = Library.Create(libName, "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        Context.Libraries.Add(lib);

        // Act
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        var list = Context.Libraries.Where(l => l.Name == libName).ToList();
        list.Should().HaveCount(1);
        var savedLib = list[0];
        Guard.Against.Null(savedLib);
        savedLib.CreatedOnUtc.Should().NotBe(null);
    }

    [Fact]
    public async Task Savechanges_Modify()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();
        var libName = "Create_" + guid;
        var lib = Library.Create(libName, "#5C6BC0", "Bookmark", LibraryBookType.Physical, s_userId).Value!;
        lib.CreatedOnUtc.Should().NotBe(null);
        Context.Libraries.Add(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        libName += "_modified";
        lib.UpdateName(libName);
        Context.Libraries.Update(lib);

        // Act
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        var list = Context.Libraries.Where(l => l.Name == libName).ToList();
        list.Should().HaveCount(1);
        var savedLib = list[0];
        Guard.Against.Null(savedLib);
        savedLib.CreatedOnUtc.Should().NotBe(null);
        savedLib.ModifiedOnUtc.Should().NotBe(null);
        Guard.Against.Null(savedLib.ModifiedOnUtc);
        savedLib.CreatedOnUtc.Should().BeBefore(savedLib.ModifiedOnUtc.Value);
    }
}
