
using Ardalis.GuardClauses;
using Base.Integration.Tests;
using Domain.Users;

namespace Persistence.Tests.Integration.Repositories;

[Collection("Database collection")]
public sealed class UsersRepositoriesTests(IntegrationTestWebAppFactory factory) : UserIntegrationTest(factory)
{
    [Fact]
    public async Task Add_ShouldAddUsr()
    {
        // Arrange
        var usr = User.Create("test@test.com", "1");

        // Act
        UserRepository.Add(usr);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        UserRepository.Count().Should().Be(1);
        var savedLib = await UserRepository.GetByIdAsync(usr.Id);
        Guard.Against.Null(savedLib);
        savedLib.Email.Should().Be("test@test.com");
        savedLib.AuthId.Should().Be("1");
    }

    [Fact]
    public async Task Add_ShouldThrowException_WhenAddusrWithSameIdTwice()
    {
        // Arrange
        var usr = User.Create("test@test.com", "1");
        UserRepository.Add(usr);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);
        UserRepository.Add(usr);
        var action = async () => { await UnitOfWork.SaveChangesAsync(CancellationToken.None); };

        // Act && Assert
        Guard.Against.Null(action);
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Update_ShouldUpdateUser()
    {
        // Arrange
        var usr = User.Create("test@test.com", "1");
        UserRepository.Add(usr);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        usr.Update("test-update@test.com", "11");
        UserRepository.Update(usr);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);


        // Assert
        UserRepository.Count().Should().Be(1);
        var savedusr = await UserRepository.GetByIdAsync(usr.Id);
        Guard.Against.Null(savedusr);
        savedusr.Email.Should().Be("test-update@test.com");
        savedusr.AuthId.Should().Be("11");
    }

    [Fact]
    public async Task Remove_ShouldRemoveusr()
    {
        // Arrange
        var usr = User.Create("test@test.com", "1");
        UserRepository.Add(usr);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        UserRepository.Remove(usr);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Assert
        UserRepository.Count().Should().Be(0);
        var savedusr = await UserRepository.GetByIdAsync(usr.Id);
        savedusr.Should().BeNull();
    }

    [Fact]
    public async Task List_ShouldListusr()
    {
        // Arrange
        var usr1 = User.Create("test-1@test.com", "1");
        var usr2 = User.Create("test-2@test.com", "2");
        var usr3 = User.Create("test-3@test.com", "3");
        UserRepository.Add(usr1);
        UserRepository.Add(usr2);
        UserRepository.Add(usr3);
        await UnitOfWork.SaveChangesAsync(CancellationToken.None);

        // Act
        var list = await UserRepository.ListAsync();

        // Assert
        UserRepository.Count().Should().Be(3);
        list.Count.Should().Be(3);
    }

}
