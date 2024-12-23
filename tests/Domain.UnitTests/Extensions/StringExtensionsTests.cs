
using Domain.Extensions;

namespace Domain.UnitTests.Extensions;
public class StringExtensionsTests
{
    [Fact]
    public void ToPascalCase_Returns_PascalCasedString()
    {
        // Arrange
        var input = "héllo_wôrld123;:,-_";
        var expectedOutput = "HelloWorld123";

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
        var input = "héllo_wôrld123;:,-_";
        var expectedOutput = "helloWorld123";

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
        var input = "éèêëÈÉÊË-ûüùÛÜÙ-ôöÔÖ-âàäÀÂÄ-îïÎÏ";
        var expectedOutput = "eeeeEEEE-uuuUUU-ooOO-aaaAAA-iiII";

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
        var input = "Hello World !";
        var sub = "wOrld";
        var expectedOutput = "Hello  !";

        // Act
        var result = input.Substract(sub);

        // Assert
        result.Should().Be(expectedOutput);
    }

    [Fact]
    public void Substract_ReturnsInput_WhenSubIsEmpty()
    {
        // Arrange
        var input = "Hello World !";
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
        var sub = "sdjqjgsqldkj";

        // Act
        var result = input.Substract(sub);

        // Assert
        result.Should().Be(input);
    }

}
