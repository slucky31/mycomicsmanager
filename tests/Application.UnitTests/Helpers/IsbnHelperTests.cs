using Application.Helpers;

namespace Application.UnitTests.Helpers;

public class IsbnHelperTests
{
    #region IsValidISBN Tests

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValidISBN_ShouldReturnFalse_WhenISBNIsNullOrEmpty(string? isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN(isbn!);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("0-306-40615-2", true)]
    [InlineData("0306406152", true)]
    [InlineData("0 306 40615 2", true)]
    public void IsValidISBN_ShouldValidateISBN10_WithVariousFormats(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("080442957X", true)]
    [InlineData("0-8044-2957-X", true)]
    [InlineData("080442957x", true)]
    [InlineData("043942089X", true)]
    public void IsValidISBN_ShouldValidateISBN10_WithXCheckDigit(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("978-0-306-40615-7", true)]
    [InlineData("9780306406157", true)]
    [InlineData("978 0 306 40615 7", true)]
    [InlineData("978 0-306-40615 7", true)]
    [InlineData("978  0  306  40615  7", true)]
    [InlineData("978-3-16-148410-0", true)]
    public void IsValidISBN_ShouldValidateISBN13_WithVariousFormats(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("12345", false)]
    [InlineData("12345678901", false)]
    [InlineData("123456789012", false)]
    [InlineData("978-0-306-40615-77", false)]
    public void IsValidISBN_ShouldReturnFalse_WhenISBNHasInvalidLength(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("978-3-16-148410-1", false)]
    public void IsValidISBN_ShouldReturnFalse_WhenInvalidChecksum(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN(isbn);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region IsValidISBN10 Tests

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValidISBN10_ShouldReturnFalse_WhenISBNIsNullOrEmpty(string? isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN10(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("0306406152", true)]
    [InlineData("0345339681", true)]
    [InlineData("0684801221", true)]
    public void IsValidISBN10_ShouldReturnTrue_WhenValidISBN10(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN10(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("080442957X", true)]
    [InlineData("043942089X", true)]
    public void IsValidISBN10_ShouldReturnTrue_WhenValidISBN10WithXCheckDigit(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN10(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("03064A6152", false)]
    [InlineData("0X06406152", false)]
    [InlineData("030640615Y", false)]
    [InlineData("0306406153", false)]
    public void IsValidISBN10_ShouldReturnFalse_WhenInvalidISBN10(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN10(isbn);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region IsValidISBN13 Tests

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValidISBN13_ShouldReturnFalse_WhenISBNIsNullOrEmpty(string? isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN13(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("9780306406157", true)]
    [InlineData("9783161484100", true)]
    [InlineData("9780451524935", true)]
    [InlineData("9780743273565", true)]
    public void IsValidISBN13_ShouldReturnTrue_WhenValidISBN13(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN13(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("978030640615A", false)]
    [InlineData("9780306406158", false)]
    [InlineData("978-0306406157", false)]
    public void IsValidISBN13_ShouldReturnFalse_WhenInvalidISBN13(string isbn, bool expected)
    {
        // Act
        var result = IsbnHelper.IsValidISBN13(isbn);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region NormalizeIsbn Tests

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NormalizeIsbn_ShouldReturnEmptyString_WhenISBNIsNullOrEmpty(string? isbn, string expected)
    {
        // Act
        var result = IsbnHelper.NormalizeIsbn(isbn!);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("978 0 306 40615 7", "9780306406157")]
    [InlineData("978 0-306-40615 7", "9780306406157")]
    [InlineData("978--0--306--40615--7", "9780306406157")]
    [InlineData("978  0  306  40615  7", "9780306406157")]
    [InlineData("9780306406157", "9780306406157")]
    public void NormalizeIsbn_ShouldRemoveDashesAndSpaces(string isbn, string expected)
    {
        // Act
        var result = IsbnHelper.NormalizeIsbn(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("080442957x", "080442957X")]
    [InlineData("0-8044-2957-x", "080442957X")]
    [InlineData("abcde12345", "ABCDE12345")]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("0-8044-2957-X", "080442957X")]
    public void NormalizeIsbn_ShouldConvertToUppercase(string isbn, string expected)
    {
        // Act
        var result = IsbnHelper.NormalizeIsbn(isbn);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Comprehensive Theory Tests

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
        var result = IsbnHelper.IsValidISBN(isbn);

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
        var result = IsbnHelper.IsValidISBN10(isbn);

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
        var result = IsbnHelper.IsValidISBN13(isbn);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("978 0 306 40615 7", "9780306406157")]
    [InlineData("9780306406157", "9780306406157")]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("080442957x", "080442957X")]
    [InlineData("0-8044-2957-X", "080442957X")]
    [InlineData("", "")]
    [InlineData("978  0--306  40615--7", "9780306406157")]
    public void NormalizeIsbn_ShouldNormalizeVariousFormats(string input, string expected)
    {
        // Act
        var result = IsbnHelper.NormalizeIsbn(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
