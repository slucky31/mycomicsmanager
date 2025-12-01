using Application.Books.Helper;

namespace Application.UnitTests.Books;

public class IsbnValidatorTests
{
    #region IsValidISBN Tests

    [Fact]
    public void IsValidISBN_ShouldReturnFalse_WhenISBNIsNull()
    {
        // Arrange
        string? isbn = null;

        // Act
        var result = IsbnValidator.IsValidISBN(isbn!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnFalse_WhenISBNIsEmpty()
    {
        // Arrange
        var isbn = string.Empty;

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN10WithDashes()
    {
        // Arrange
        var isbn = "0-306-40615-2";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN10WithoutDashes()
    {
        // Arrange
        var isbn = "0306406152";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN10WithXCheckDigit()
    {
        // Arrange - Valid ISBN-10 with X check digit
        var isbn = "080442957X";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN10WithDashesAndX()
    {
        // Arrange
        var isbn = "0-8044-2957-X";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN10WithLowercaseXConverted()
    {
        // Arrange - Lowercase x gets converted to uppercase
        var isbn = "080442957x";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN13WithDashes()
    {
        // Arrange
        var isbn = "978-0-306-40615-7";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN13WithoutDashes()
    {
        // Arrange
        var isbn = "9780306406157";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN13WithSpaces()
    {
        // Arrange
        var isbn = "978 0 306 40615 7";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN10WithSpaces()
    {
        // Arrange
        var isbn = "0 306 40615 2";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnFalse_WhenISBNHasInvalidLength()
    {
        // Arrange
        var isbn = "12345";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnFalse_WhenISBNHas11Digits()
    {
        // Arrange
        var isbn = "12345678901";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnFalse_WhenISBNHas12Digits()
    {
        // Arrange
        var isbn = "123456789012";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnTrue_WhenValidISBN13Example1()
    {
        // Arrange
        var isbn = "978-3-16-148410-0";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnFalse_WhenInvalidChecksum()
    {
        // Arrange - Invalid checksum
        var isbn = "978-3-16-148410-1";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN_ShouldReturnFalse_WhenISBNHas14DigitsAfterCleaning()
    {
        // Arrange
        var isbn = "978-0-306-40615-77";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN_ShouldHandleMixedDashesAndSpaces()
    {
        // Arrange
        var isbn = "978 0-306-40615 7";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN_ShouldRemoveMultipleSpaces()
    {
        // Arrange
        var isbn = "978  0  306  40615  7";

        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsValidISBN10 Tests

    [Fact]
    public void IsValidISBN10_ShouldReturnFalse_WhenISBNIsNull()
    {
        // Arrange
        string? isbn = null;

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnFalse_WhenISBNIsEmpty()
    {
        // Arrange
        var isbn = string.Empty;

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnTrue_WhenValidISBN10()
    {
        // Arrange
        var isbn = "0306406152";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnTrue_WhenValidISBN10WithXCheckDigit()
    {
        // Arrange - Valid ISBN-10 with X check digit
        var isbn = "080442957X";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnTrue_WhenAnotherValidISBN10WithX()
    {
        // Arrange
        var isbn = "043942089X";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnFalse_WhenISBNContainsLetters()
    {
        // Arrange
        var isbn = "03064A6152";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnFalse_WhenXIsNotLastCharacter()
    {
        // Arrange
        var isbn = "0X06406152";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnFalse_WhenInvalidChecksum()
    {
        // Arrange
        var isbn = "0306406153";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnFalse_WhenLastCharacterIsInvalid()
    {
        // Arrange
        var isbn = "030640615Y";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnTrue_WhenValidISBN10Example1()
    {
        // Arrange
        var isbn = "0345339681";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN10_ShouldReturnTrue_WhenValidISBN10Example2()
    {
        // Arrange
        var isbn = "0684801221";

        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsValidISBN13 Tests

    [Fact]
    public void IsValidISBN13_ShouldReturnFalse_WhenISBNIsNull()
    {
        // Arrange
        string? isbn = null;

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN13_ShouldReturnFalse_WhenISBNIsEmpty()
    {
        // Arrange
        var isbn = string.Empty;

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN13_ShouldReturnTrue_WhenValidISBN13()
    {
        // Arrange
        var isbn = "9780306406157";

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN13_ShouldReturnTrue_WhenValidISBN13Example1()
    {
        // Arrange
        var isbn = "9783161484100";

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN13_ShouldReturnTrue_WhenValidISBN13Example2()
    {
        // Arrange
        var isbn = "9780451524935";

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN13_ShouldReturnTrue_WhenValidISBN13Example3()
    {
        // Arrange
        var isbn = "9780743273565";

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidISBN13_ShouldReturnFalse_WhenISBNContainsLetters()
    {
        // Arrange
        var isbn = "978030640615A";

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN13_ShouldReturnFalse_WhenInvalidChecksum()
    {
        // Arrange
        var isbn = "9780306406158";

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidISBN13_ShouldReturnFalse_WhenContainsSpecialCharacters()
    {
        // Arrange - Dashes not removed by ISBN13 validator directly
        var isbn = "978-0306406157";

        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Theory Tests

    [Theory]
    [InlineData("0-306-40615-2", true)]
    [InlineData("978-0-306-40615-7", true)]
    [InlineData("invalid-isbn", false)]
    [InlineData("123", false)]
    [InlineData("12345678901234", false)]
    [InlineData("", false)]
    [InlineData("080442957X", true)]
    [InlineData("080442957x", true)]
    public void IsValidISBN_ShouldValidateMultipleFormats(string isbn, bool expected)
    {
        // Act
        var result = IsbnValidator.IsValidISBN(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("0306406152", true)]
    [InlineData("080442957X", true)]
    [InlineData("043942089X", true)]
    [InlineData("0684801221", true)]
    [InlineData("0345339681", true)]
    [InlineData("0306406153", false)]
    [InlineData("03064A6152", false)]
    public void IsValidISBN10_ShouldValidateMultipleISBN10s(string isbn, bool expected)
    {
        // Act
        var result = IsbnValidator.IsValidISBN10(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("9780306406157", true)]
    [InlineData("9783161484100", true)]
    [InlineData("9780451524935", true)]
    [InlineData("9780743273565", true)]
    [InlineData("9780306406158", false)]
    [InlineData("978030640615A", false)]
    public void IsValidISBN13_ShouldValidateMultipleISBN13s(string isbn, bool expected)
    {
        // Act
        var result = IsbnValidator.IsValidISBN13(isbn);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
