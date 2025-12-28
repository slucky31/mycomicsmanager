
using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Extensions;
using Domain.Libraries;

namespace Persistence.Tests.Integration.Repositories;

[Collection("DatabaseCollectionTests")]
public sealed class LibraryRepositoriesTests(IntegrationTestWebAppFactory factory) : LibraryIntegrationTest(factory)
{
    [Fact]
    public async Task Add_ShouldAddLibAsync()
    {
        // Arrange
        var lib = Library.Create("name");

        // Act
        LibraryRepository.Add(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        LibraryRepository.Count().Should().Be(1);
        var savedLib = await LibraryRepository.GetByIdAsync(lib.Id);
        Guard.Against.Null(savedLib);
        savedLib.Name.Should().Be("name");
        savedLib.RelativePath.Should().Be("name".RemoveDiacritics().ToUpperInvariant());
    }

    [Fact]
    public async Task Add_ShouldThrowException_WhenAddLibWithSameIdTwiceAsync()
    {
        // Arrange
        var lib = Library.Create("name");
        LibraryRepository.Add(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        LibraryRepository.Add(lib);
        var action = async () => await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act && Assert
        Guard.Against.Null(action);
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Update_ShouldUpdateLibAsync()
    {
        // Arrange
        var lib = Library.Create("name");
        LibraryRepository.Add(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        lib.Update("name-update");
        LibraryRepository.Update(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);


        // Assert
        LibraryRepository.Count().Should().Be(1);
        var savedLib = await LibraryRepository.GetByIdAsync(lib.Id);
        Guard.Against.Null(savedLib);
        savedLib.Name.Should().Be("name-update");
        savedLib.RelativePath.Should().Be("name-update".RemoveDiacritics().ToUpperInvariant());
    }

    [Fact]
    public async Task Remove_ShouldRemoveLibAsync()
    {
        // Arrange
        var lib = Library.Create("name");
        LibraryRepository.Add(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        LibraryRepository.Remove(lib);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        LibraryRepository.Count().Should().Be(0);
        var savedLib = await LibraryRepository.GetByIdAsync(lib.Id);
        savedLib.Should().BeNull();
    }

    [Fact]
    public async Task List_ShouldListLibAsync()
    {
        // Arrange
        var lib1 = Library.Create("name");
        var lib2 = Library.Create("name2");
        var lib3 = Library.Create("name3");
        LibraryRepository.Add(lib1);
        LibraryRepository.Add(lib2);
        LibraryRepository.Add(lib3);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var list = await LibraryRepository.ListAsync();

        // Assert
        LibraryRepository.Count().Should().Be(3);
        list.Count.Should().Be(3);
    }

}
