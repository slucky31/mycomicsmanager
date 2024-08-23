using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Libraries;
using Domain.Primitives;

namespace Persistence.Tests.Integration.Queries;
public class LibraryReadServiceTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private readonly Library lib1 = Library.Create("Bande dessinées");
    private readonly Library lib2 = Library.Create("comics");
    private readonly Library lib3 = Library.Create("manga");
    private readonly Library lib4 = Library.Create("manhwa");
    private readonly Library lib5 = Library.Create("webcomics");
    private readonly Library lib6 = Library.Create("graphics novels");
    private readonly Library lib7 = Library.Create("comics strips");
    private readonly List<Library> libs = [];

    private async Task CreateLibraries()
    {

        LibraryRepository.Add(lib1);
        LibraryRepository.Add(lib2);
        LibraryRepository.Add(lib3);
        LibraryRepository.Add(lib4);
        LibraryRepository.Add(lib5);
        LibraryRepository.Add(lib6);
        LibraryRepository.Add(lib7);

        libs.Clear();
        libs.Add(lib1);
        libs.Add(lib2);
        libs.Add(lib3);
        libs.Add(lib4);
        libs.Add(lib5);
        libs.Add(lib6);
        libs.Add(lib7);

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

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnAllItemsPagedList_WhenSearchTermIsNull()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync(null, null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(7);
        pagedList.Items.Should().Contain(l => l.Id == lib1.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib2.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib3.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib4.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib5.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib6.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib7.Id);
    }

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnAllItemsPagedList_WhenSearchTermIsEmpty()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync("", null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(7);
        pagedList.Items.Should().Contain(l => l.Id == lib1.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib2.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib3.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib4.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib5.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib6.Id);
        pagedList.Items.Should().Contain(l => l.Id == lib7.Id);
    }

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnItemsPagedListOrderById_WhenSortColumnIsNull()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync(null, null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(7);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Id).Should().ContainInOrder(libs.OrderBy(l => l.Id).Select(l => l.Id).ToArray());

    }

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnItemsPagedListOrderById_WhenSortColumnIsId()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync(null, LibrariesColumn.Id, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(7);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Id).Should().ContainInOrder(libs.OrderBy(l => l.Id).Select(l => l.Id).ToArray());

    }

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnItemsPagedListOrderByName_WhenSortColumnIsName()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync(null, LibrariesColumn.Name, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(7);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Name).Should().ContainInOrder(libs.OrderBy(l => l.Name).Select(l => l.Name).ToArray());

    }

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnItemsPagedListOrderDescendingByName_WhenSortColumnIsNameAndSorterOrderIsDesc()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync(null, LibrariesColumn.Name, SortOrder.Descending, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(7);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Name).Should().ContainInOrder(libs.OrderByDescending(l => l.Name).Select(l => l.Name).ToArray());

    }

}
