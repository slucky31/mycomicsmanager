
using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Libraries;

namespace Persistence.Tests;

[Collection("DatabaseCollectionTests")]
public class UnitOfWorkTests(IntegrationTestWebAppFactory factory) : LibraryIntegrationTest(factory)
{
    [Fact]
    public async Task Savechanges_Create()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();
        var libName = "Create_" + guid;
        var lib = Library.Create(libName);
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
        var lib = Library.Create(libName);
        lib.CreatedOnUtc.Should().NotBe(null);
        Context.Libraries.Add(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        libName += "_modified";
        lib.Update(libName);
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
