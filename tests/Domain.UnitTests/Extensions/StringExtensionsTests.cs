
using Domain.Extensions;

namespace Domain.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void ToPascalCase_Returns_PascalCasedString()
    {
        // Arrange
        const string input = "héllo_wôrld123;:,-_";
        const string expectedOutput = "HelloWorld123";

        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void ToPascalCase_ReturnsEmptyString_WhenGivenEmptyString()
    {
        // Arrange
        var input = String.Empty;
        var expectedOutput = String.Empty;

        // Act
        var result = input.ToPascalCase();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void ToCamlCase_Returns_CorrectString()
    {
        // Arrange
        const string input = "héllo_wôrld123;:,-_";
        const string expectedOutput = "helloWorld123";

        // Act
        var result = input.ToCamlCase();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void ToCamlCase_ReturnsEmptyString_WhenGivenEmptyString()
    {
        // Arrange
        var input = String.Empty;
        var expectedOutput = String.Empty;

        // Act
        var result = input.ToCamlCase();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void RemoveDiacritics_RemovesDiacritics()
    {
        // Arrange
        const string input = "éèêëÈÉÊË-ûüùÛÜÙ-ôöÔÖ-âàäÀÂÄ-îïÎÏ";
        const string expectedOutput = "eeeeEEEE-uuuUUU-ooOO-aaaAAA-iiII";

        // Act
        var result = input.RemoveDiacritics();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void RemoveDiacritics_ReturnsEmptyString_WhenGivenEmptyString()
    {
        // Arrange
        var input = String.Empty;
        var expectedOutput = String.Empty;

        // Act
        var result = input.RemoveDiacritics();

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void Substract_ReturnsSubstract()
    {
        // Arrange
        const string input = "Hello World !";
        const string sub = "wOrld";
        const string expectedOutput = "Hello  !";

        // Act
        var result = input.Substract(sub);

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void Substract_ReturnsInput_WhenSubIsEmpty()
    {
        // Arrange
        const string input = "Hello World !";
        var sub = String.Empty;

        // Act
        var result = input.Substract(sub);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void Substract_ReturnsEmpty_WhenIsEmpty()
    {
        // Arrange
        var input = String.Empty;
        const string sub = "sdjqjgsqldkj";

        // Act
        var result = input.Substract(sub);

        // Assert
        result.Should().Be(input);
    }

}
