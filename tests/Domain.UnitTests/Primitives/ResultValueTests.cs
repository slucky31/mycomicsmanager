using Domain.Primitives;

namespace Domain.UnitTests.Primitives;

public class ResultValueTests
{
    // Arrange
    // Act
    // Assert

    [Fact]
    public void Success_Should_SetIsSuccesToTrueAndReturnValue()
    {
        // Arrange
        const string value = "the tested value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().Be(true);
        result.IsFailure.Should().Be(false);
        result.Error.Should().BeNull();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_Should_SetIsFailureToTrueAndSetError()
    {
        // Arrange
        var error = new TError("500", "BSOD");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        result.IsSuccess.Should().Be(false);
        result.IsFailure.Should().Be(true);
        result.Error.Should().Be(error);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Implicit_Should_ConvertFromValue()
    {
        // Arrange
        const string value = "the tested value";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().Be(true);
        result.IsFailure.Should().Be(false);
        result.Value.Should().Be(value);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Implicit_Should_ConvertFromTError()
    {
        // Arrange
        var error = new TError("500", "BSOD");

        // Act
        Result<string> result = error;

        // Assert
        result.IsSuccess.Should().Be(false);
        result.IsFailure.Should().Be(true);
        result.Error.Should().Be(error);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void ToResult_Should_ConvertFromTError()
    {
        // Arrange
        var error = new TError("500", "BSOD");

        // Act
        var result = Result<string>.ToResult(error);

        // Assert
        result.IsSuccess.Should().Be(false);
        result.IsFailure.Should().Be(true);
        result.Error.Should().Be(error);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void ToResult_Should_ConvertFromValue()
    {
        // Arrange
        const string value = "the tested value";

        // Act
        var result = Result<string>.ToResult(value);

        // Assert
        result.IsSuccess.Should().Be(true);
        result.IsFailure.Should().Be(false);
        result.Value.Should().Be(value);
        result.Error.Should().BeNull();
    }

}
