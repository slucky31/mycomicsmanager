using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Primitives;
using Domain.Users;

namespace Persistence.Tests.Integration.Queries;

[Collection("DatabaseCollectionTests")]
public class UserReadServiceTests(IntegrationTestWebAppFactory factory) : UserIntegrationTest(factory)
{
    private readonly User _usr1 = User.Create("usr1@test.com", "1");
    private readonly User _usr2 = User.Create("usr2@test.com", "2");
    private readonly User _usr3 = User.Create("usr3@test.com", "3");
    private readonly User _usr4 = User.Create("usr4-comics@test.com", "4");
    private readonly User _usr5 = User.Create("usr5-comics@test.com", "5");

    private readonly List<User> _users = [];

    private async Task CreateUsersAsync()
    {

        UserRepository.Add(_usr1);
        UserRepository.Add(_usr2);
        UserRepository.Add(_usr3);
        UserRepository.Add(_usr4);
        UserRepository.Add(_usr5);

        _users.Clear();
        _users.Add(_usr1);
        _users.Add(_usr2);
        _users.Add(_usr3);
        _users.Add(_usr4);
        _users.Add(_usr5);

        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnPagedListAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.Email, SortOrder.Ascending, 1, 2);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(2);
        pagedList.Items.Should().Contain(u => u.Id == _usr1.Id);
        pagedList.Items.Should().Contain(u => u.Id == _usr2.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnPagedList_WhichContainsComicsInNameAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync("comics", null, null, 1, 3);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(2);
        pagedList.Items.Should().Contain(u => u.Id == _usr4.Id);
        pagedList.Items.Should().Contain(u => u.Id == _usr5.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnAllItemsPagedList_WhenSearchTermIsNullAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        pagedList.Items.Should().Contain(l => l.Id == _usr1.Id);
        pagedList.Items.Should().Contain(l => l.Id == _usr2.Id);
        pagedList.Items.Should().Contain(l => l.Id == _usr3.Id);
        pagedList.Items.Should().Contain(l => l.Id == _usr4.Id);
        pagedList.Items.Should().Contain(l => l.Id == _usr5.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnAllItemsPagedList_WhenSearchTermIsEmptyAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync("", null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        pagedList.Items.Should().Contain(l => l.Id == _usr1.Id);
        pagedList.Items.Should().Contain(l => l.Id == _usr2.Id);
        pagedList.Items.Should().Contain(l => l.Id == _usr3.Id);
        pagedList.Items.Should().Contain(l => l.Id == _usr4.Id);
        pagedList.Items.Should().Contain(l => l.Id == _usr5.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderById_WhenSortColumnIsNullAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Id).Should().ContainInOrder(_users.OrderBy(l => l.Id).Select(l => l.Id).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderById_WhenSortColumnIsIdAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.Id, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Id).Should().ContainInOrder(_users.OrderBy(l => l.Id).Select(l => l.Id).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderByEmail_WhenSortColumnIsEmailAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.Email, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Email).Should().ContainInOrder(_users.OrderBy(l => l.Email).Select(l => l.Email).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderDescendingByEmail_WhenSortColumnIsEmailAndSortOrderIsDescAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.Email, SortOrder.Descending, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Email).Should().ContainInOrder(_users.OrderByDescending(l => l.Email).Select(l => l.Email).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderByAuthId_WhenSortColumnIsAuthIdAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.AuthId, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.AuthId).Should().ContainInOrder(_users.OrderBy(l => l.AuthId).Select(l => l.AuthId).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderDescendingByAuthId_WhenSortColumnIsAuthIdAndSorterOrderIsDescAsync()
    {
        // Arrange
        await CreateUsersAsync();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.AuthId, SortOrder.Descending, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.AuthId).Should().ContainInOrder(_users.OrderByDescending(l => l.AuthId).Select(l => l.AuthId).ToArray());

    }

    [Fact]
    public async Task GetUserByAuthIdAndEmail_WithValidData_ReturnsUserAsync()
    {
        // Arrange
        var user = User.Create("test@test.com", "123456");
        UserRepository.Add(user);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await UserReadService.GetUserByAuthIdAndEmail(user.Email, user.AuthId);

        // Assert        
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(user.Email);
        result.Value.AuthId.Should().Be(user.AuthId);
    }

    [Fact]
    public async Task GetUserByAuthIdAndEmail_WithInvalidData_ReturnsBadRequestAsync()
    {
        // Arrange
        const string email = "";
        const string authId = "123456";

        // Act
        var result = await UserReadService.GetUserByAuthIdAndEmail(email, authId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.BadRequest);
    }

    [Fact]
    public async Task GetUserByAuthIdOrEmail_WithNonExistingUser_ReturnsNotFoundAsync()
    {
        // Arrange
        var user = User.Create("test@test.com", "123456");

        // Act
        var result = await UserReadService.GetUserByAuthIdAndEmail(user.Email, user.AuthId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
    }

    [Fact]
    public async Task GetUserByEmail_WhenEmailIsEmpty_ReturnsBadRequestAsync()
    {
        // Arrange
        var email = string.Empty;

        // Act
        var result = await UserReadService.GetUserByEmail(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.BadRequest);
    }

    [Fact]
    public async Task GetUserByEmail_WhenUserNotFound_ReturnsNotFoundAsync()
    {
        // Arrange
        const string email = "test@example.com";

        // Act
        var result = await UserReadService.GetUserByEmail(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
    }

    [Fact]
    public async Task GetUserByEmail_WhenUserFound_ReturnsUserAsync()
    {
        // Arrange
        const string email = "test@example.com";
        var user = User.Create(email, "123456");
        UserRepository.Add(user);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await UserReadService.GetUserByEmail(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Guard.Against.Null(result.Value);
        result.Value.Email.Should().Be(user.Email);
        result.Value.AuthId.Should().Be(user.AuthId);
    }

}
