
using Domain.Extensions;

namespace Domain.UnitTests.Extensions;
public class StringExtensionsTests
{
    [Fact]
    public void ToPascalCase_Returns_PascalCasedString()
    {
        // Arrange
        string input = "héllo_wôrld123;:,-_";
        string expectedOutput = "HelloWorld123";

        // Act
        string? result = input.ToPascalCase();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void ToPascalCase_ReturnsEmptyString_WhenGivenEmptyString()
    {
        // Arrange
        string input = String.Empty;
        string expectedOutput = String.Empty;

        // Act
        string? result = input.ToPascalCase();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void ToCamlCase_Returns_CorrectString()
    {
        // Arrange
        string input = "héllo_wôrld123;:,-_";
        string expectedOutput = "helloWorld123";

        // Act
        string? result = input.ToCamlCase();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void ToCamlCase_ReturnsEmptyString_WhenGivenEmptyString()
    {
        // Arrange
        string? input = String.Empty;
        string? expectedOutput = String.Empty;

        // Act
        string? result = input.ToCamlCase();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void RemoveDiacritics_RemovesDiacritics()
    {
        // Arrange
        string input = "éèêëÈÉÊË-ûüùÛÜÙ-ôöÔÖ-âàäÀÂÄ-îïÎÏ";
        string expectedOutput = "eeeeEEEE-uuuUUU-ooOO-aaaAAA-iiII";

        // Act
        string? result = input.RemoveDiacritics();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void RemoveDiacritics_ReturnsEmptyString_WhenGivenEmptyString()
    {
        // Arrange
        string? input = String.Empty;
        string? expectedOutput = String.Empty;

        // Act
        string? result = input.RemoveDiacritics();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void Substract_ReturnsSubstract()
    {
        // Arrange
        string? input = "Hello World !";
        string? sub = "wOrld";
        string? expectedOutput = "Hello  !";

        // Act
        string? result = input.Substract(sub);

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void Substract_ReturnsInput_WhenSubIsEmpty()
    {
        // Arrange
        string? input = "Hello World !";
        string? sub = String.Empty;

        // Act
        string? result = input.Substract(sub);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void Substract_ReturnsEmpty_WhenIsEmpty()
    {
        // Arrange
        string? input = String.Empty;
        string? sub = "sdjqjgsqldkj";

        // Act
        string? result = input.Substract(sub);

        // Assert
        result.Should().Be(input);
    }

}
