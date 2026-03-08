using Domain.Extensions;

namespace Domain.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("héllo_wôrld123;:,-_", "HelloWorld123")]
    [InlineData("", "")]
    public void ToPascalCase_Should_ReturnExpectedString_WhenInputIsProvided(string input, string expected)
    {
        input.ToPascalCase().Should().Be(expected);
    }

    [Theory]
    [InlineData("héllo_wôrld123;:,-_", "helloWorld123")]
    [InlineData("", "")]
    public void ToCamlCase_Should_ReturnExpectedString_WhenInputIsProvided(string input, string expected)
    {
        input.ToCamlCase().Should().Be(expected);
    }

    [Theory]
    [InlineData("éèêëÈÉÊË-ûüùÛÜÙ-ôöÔÖ-âàäÀÂÄ-îïÎÏ", "eeeeEEEE-uuuUUU-ooOO-aaaAAA-iiII")]
    [InlineData("", "")]
    public void RemoveDiacritics_Should_ReturnExpectedString_WhenInputIsProvided(string input, string expected)
    {
        input.RemoveDiacritics().Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World !", "wOrld", "Hello  !")]
    [InlineData("Hello World !", "", "Hello World !")]
    [InlineData("", "sdjqjgsqldkj", "")]
    [InlineData("Hello World !", null, "Hello World !")]
    public void Substract_Should_ReturnExpectedString_WhenInputIsProvided(string input, string? sub, string expected)
    {
        input.Substract(sub).Should().Be(expected);
    }
}
