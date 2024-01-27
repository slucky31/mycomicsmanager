using Domain.Primitives;

namespace Domain.UnitTests;

public class ResultTests
{
    // Arrange
    // Act
    // Assert

    [Fact]
    public void Success_Should_SetIsSuccesToTrue()
    {
        // Arrange
        // Act
        Result result = Result.Success();

        // Assert
        result.IsSuccess.Should().Be(true);
        result.IsFailure.Should().Be(false);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_Should_SetIsFailureToTrue()
    {
        // Arrange
        TError error = new TError("500", "BSOD");

        // Act
        Result result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().Be(false);
        result.IsFailure.Should().Be(true);
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Implicit_Should_ConvertFromTError()
    {
        // Arrange
        TError error = new TError("500", "BSOD");

        // Act
        Result result = error;

        // Assert
        result.IsSuccess.Should().Be(false);
        result.IsFailure.Should().Be(true);
        result.Error.Should().Be(error);
    }

}
