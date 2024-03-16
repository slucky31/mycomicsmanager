using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Primitives;
using Domain.Users;

namespace Persistence.Tests.Integration.Queries;
public class UserReadServiceTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private readonly User usr1 = User.Create("usr1@test.com", "1");
    private readonly User usr2 = User.Create("usr2@test.com", "2");
    private readonly User usr3 = User.Create("usr3@test.com", "3");
    private readonly User usr4 = User.Create("usr4-comics@test.com", "4");
    private readonly User usr5 = User.Create("usr5-comics@test.com", "5");

    private readonly List<User> users = [];

    private async Task CreateUsers()
    {

        UserRepository.Add(usr1);
        UserRepository.Add(usr2);
        UserRepository.Add(usr3);
        UserRepository.Add(usr4);
        UserRepository.Add(usr5);


        users.Clear();
        users.Add(usr1);
        users.Add(usr2);
        users.Add(usr3);
        users.Add(usr4);
        users.Add(usr5);        

        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnPagedList()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, null, null, 1, 2);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(2);
        pagedList.Items.Should().Contain(u => u.Id == usr1.Id);
        pagedList.Items.Should().Contain(u => u.Id == usr2.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnPagedList_WichContainsComicsInName()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync("comics", null, null, 1, 3);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(2);
        pagedList.Items.Should().Contain(u => u.Id == usr4.Id);
        pagedList.Items.Should().Contain(u => u.Id == usr5.Id);        
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnAllItemsPagedList_WhenSearchTermIsNull()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        pagedList.Items.Should().Contain(l => l.Id == usr1.Id);
        pagedList.Items.Should().Contain(l => l.Id == usr2.Id);
        pagedList.Items.Should().Contain(l => l.Id == usr3.Id);
        pagedList.Items.Should().Contain(l => l.Id == usr3.Id);
        pagedList.Items.Should().Contain(l => l.Id == usr5.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnAllItemsPagedList_WhenSearchTermIsEmpty()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync("", null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        pagedList.Items.Should().Contain(l => l.Id == usr1.Id);
        pagedList.Items.Should().Contain(l => l.Id == usr2.Id);
        pagedList.Items.Should().Contain(l => l.Id == usr3.Id);
        pagedList.Items.Should().Contain(l => l.Id == usr3.Id);
        pagedList.Items.Should().Contain(l => l.Id == usr5.Id);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderById_WhenSortColumnIsNull()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, null, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Id).Should().ContainInOrder(users.OrderBy(l => l.Id).Select(l => l.Id).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderById_WhenSortColumnIsId()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.Id, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Id).Should().ContainInOrder(users.OrderBy(l => l.Id).Select(l => l.Id).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderByEmail_WhenSortColumnIsEmail()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.Email, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Email).Should().ContainInOrder(users.OrderBy(l => l.Email).Select(l => l.Email).ToArray());

    } 

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderDescendingByEmail_WhenSortColumnIsEmailAndSorterOrderIsDesc()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.Email, SortOrder.Descending, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.Email).Should().ContainInOrder(users.OrderByDescending(l => l.Email).Select(l => l.Email).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderByAuhId_WhenSortColumnIsAuthId()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.AuthId, null, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.AuthId).Should().ContainInOrder(users.OrderBy(l => l.AuthId).Select(l => l.AuthId).ToArray());

    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnItemsPagedListOrderDescendingByAuthId_WhenSortColumnIsAuthIdAndSorterOrderIsDesc()
    {
        // Arrange
        await CreateUsers();

        // Act
        var pagedList = await UserReadService.GetUsersAsync(null, UsersColumn.AuthId, SortOrder.Descending, 1, 10);

        //Assert
        pagedList.Should().NotBeNull();
        pagedList.Items.Should().HaveCount(5);
        Guard.Against.Null(pagedList.Items);
        pagedList.Items.Select(l => l.AuthId).Should().ContainInOrder(users.OrderByDescending(l => l.AuthId).Select(l => l.AuthId).ToArray());

    }

    [Fact]
    public async Task GetUserByAuthIdOrEmail_WithValidData_ReturnsUser()
    {
        // Arrange
        var user = User.Create("test@test.com", "123456");
        UserRepository.Add(user);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await UserReadService.GetUserByAuthIdOrEmail(user.Email, user.AuthId);

        // Assert        
        Guard.Against.Null(result.Value);
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(user.Email);
        result.Value.AuthId.Should().Be(user.AuthId);
    }

    [Fact]
    public async Task GetUserByAuthIdOrEmail_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var email = "";
        var authId = "123456";

        // Act
        var result = await UserReadService.GetUserByAuthIdOrEmail(email, authId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.BadRequest);
    }

    [Fact]
    public async Task GetUserByAuthIdOrEmail_WithNonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var user = User.Create("test@test.com", "123456");

        // Act
        var result = await UserReadService.GetUserByAuthIdOrEmail(user.Email, user.AuthId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
    }

    [Fact]
    public async Task GetUserByEmail_WhenEmailIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        string email = string.Empty;

        // Act
        var result = await UserReadService.GetUserByEmail(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.BadRequest);
    }

    [Fact]
    public async Task GetUserByEmail_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        string email = "test@example.com";

        // Act
        var result = await UserReadService.GetUserByEmail(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UsersError.NotFound);
    }

    [Fact]
    public async Task GetUserByEmail_WhenUserFound_ReturnsUser()
    {
        // Arrange
        string email = "test@example.com";
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
