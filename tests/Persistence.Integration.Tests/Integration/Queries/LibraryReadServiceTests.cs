using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Libraries;
using Domain.Primitives;

namespace Persistence.Tests.Integration.Queries;

[Collection("DatabaseCollectionTests")]
public class LibraryReadServiceTests(IntegrationTestWebAppFactory factory) : LibraryIntegrationTest(factory)
{
    private readonly Library _lib1 = Library.Create("1-Bande dessinées");
    private readonly Library _lib2 = Library.Create("2-comics");
    private readonly Library _lib3 = Library.Create("3-manga");
    private readonly Library _lib4 = Library.Create("4-manhwa");
    private readonly Library _lib5 = Library.Create("5-webcomics");
    private readonly Library _lib6 = Library.Create("6-graphics novels");
    private readonly Library _lib7 = Library.Create("7-comics strips");
    private readonly List<Library> _libs = [];

    private async Task CreateLibraries()
    {

        LibraryRepository.Add(_lib1);
        LibraryRepository.Add(_lib2);
        LibraryRepository.Add(_lib3);
        LibraryRepository.Add(_lib4);
        LibraryRepository.Add(_lib5);
        LibraryRepository.Add(_lib6);
        LibraryRepository.Add(_lib7);

        _libs.Clear();
        _libs.Add(_lib1);
        _libs.Add(_lib2);
        _libs.Add(_lib3);
        _libs.Add(_lib4);
        _libs.Add(_lib5);
        _libs.Add(_lib6);
        _libs.Add(_lib7);

        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetLibrariesAsync_ShouldReturnPagedList()
    {
        // Arrange
        await CreateLibraries();

        // Act
        var pagedList = await LibraryReadService.GetLibrariesAsync(null, LibrariesColumn.Name, SortOrder.Ascending, 1, 2);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(2);
        pagedList.Items.Should().Contain(l => l.Id == _lib1.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib2.Id);
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
        pagedList.Items.Should().Contain(l => l.Id == _lib2.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib5.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib7.Id);
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
        pagedList.Items.Should().Contain(l => l.Id == _lib1.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib2.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib3.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib4.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib5.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib6.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib7.Id);
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
        pagedList.Items.Should().Contain(l => l.Id == _lib1.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib2.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib3.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib4.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib5.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib6.Id);
        pagedList.Items.Should().Contain(l => l.Id == _lib7.Id);
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
        pagedList.Items.Select(l => l.Id).Should().ContainInOrder(_libs.OrderBy(l => l.Id).Select(l => l.Id).ToArray());

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
        pagedList.Items.Select(l => l.Id).Should().ContainInOrder(_libs.OrderBy(l => l.Id).Select(l => l.Id).ToArray());

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
        pagedList.Items.Select(l => l.Name).Should().ContainInOrder(_libs.OrderBy(l => l.Name).Select(l => l.Name).ToArray());

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
        pagedList.Items.Select(l => l.Name).Should().ContainInOrder(_libs.OrderByDescending(l => l.Name).Select(l => l.Name).ToArray());

    }

}
