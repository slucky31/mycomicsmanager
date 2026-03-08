using Domain.Users;

namespace Domain.UnitTests.Users;

public class UserTests
{
    // -------------------------------------------------------
    // Update
    // -------------------------------------------------------

    [Fact]
    public void Update_Should_UpdateEmailAndAuthId_WhenValuesProvided()
    {
        // Arrange
        var user = User.Create("old@example.com", "old-auth-id");
        const string newEmail = "new@example.com";
        const string newAuthId = "new-auth-id";

        // Act
        user.Update(newEmail, newAuthId);

        // Assert
        user.Email.Should().Be(newEmail);
        user.AuthId.Should().Be(newAuthId);
    }

    [Fact]
    public void Update_Should_NotChangeId_WhenCalled()
    {
        // Arrange
        var user = User.Create("old@example.com", "old-auth-id");
        var originalId = user.Id;

        // Act
        user.Update("new@example.com", "new-auth-id");

        // Assert
        user.Id.Should().Be(originalId);
    }

    [Fact]
    public void Update_Should_OverwritePreviousValues_WhenCalledMultipleTimes()
    {
        // Arrange
        var user = User.Create("first@example.com", "first-auth-id");
        user.Update("second@example.com", "second-auth-id");

        // Act
        user.Update("third@example.com", "third-auth-id");

        // Assert
        user.Email.Should().Be("third@example.com");
        user.AuthId.Should().Be("third-auth-id");
    }

    [Fact]
    public void Update_Should_AllowEmptyValues_WhenInputsAreEmpty()
    {
        // Arrange
        var user = User.Create("old@example.com", "old-auth-id");

        // Act
        user.Update(string.Empty, string.Empty);

        // Assert
        user.Email.Should().BeEmpty();
        user.AuthId.Should().BeEmpty();
    }

    [Fact]
    public void Update_Should_AllowNullValues_WhenInputsAreNull()
    {
        // Arrange
        var user = User.Create("old@example.com", "old-auth-id");

        // Act
        user.Update(null!, null!);

        // Assert
        user.Email.Should().BeNull();
        user.AuthId.Should().BeNull();
    }
}
