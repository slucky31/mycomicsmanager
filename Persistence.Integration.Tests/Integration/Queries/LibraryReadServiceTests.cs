using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Libraries;
using Persistence.Repositories;

namespace Persistence.Tests.Integration.Queries;
public class LibraryReadServiceTests : BaseIntegrationTest
{
    private readonly Library lib1 = Library.Create("Bande dessinées", "bd");
    private readonly Library lib2 = Library.Create("comics", "comics");
    private readonly Library lib3 = Library.Create("manga", "manga");
    private readonly Library lib4 = Library.Create("manhwa", "manhwa");
    private readonly Library lib5 = Library.Create("webcomics", "webcomics");
    private readonly Library lib6 = Library.Create("graphics novels", "graphics novels");
    private readonly Library lib7 = Library.Create("comics strips", "comics strips");

    public LibraryReadServiceTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    private async Task CreateLibraries()
    {

        LibraryRepository.Add(lib1);
        LibraryRepository.Add(lib2);
        LibraryRepository.Add(lib3);
        LibraryRepository.Add(lib4);
        LibraryRepository.Add(lib5);
        LibraryRepository.Add(lib6);
        LibraryRepository.Add(lib7);

        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnPagedList()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync(null, null, null, 1, 2);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(2);
        pagedList.Items.Should().Contain(l => l.Id == lib1.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib2.Id);
    }

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnPagedList_WichContainsComicsInName()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync("comics", null, null, 1, 3);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(3);
        pagedList.Items.Should().Contain(l => l.Id == lib2.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib5.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib7.Id);
    }



}
