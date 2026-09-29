using Domain.Extensions;

namespace Domain.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("éèêëÈÉÊË-ûüùÛÜÙ-ôöÔÖ-âàäÀÂÄ-îïÎÏ", "eeeeEEEE-uuuUUU-ooOO-aaaAAA-iiII")]
    [InlineData("", "")]
    public void RemoveDiacritics_Should_ReturnExpectedString_WhenInputIsProvided(string input, string expected)
    {
        input.RemoveDiacritics().Should().Be(expected);
    }
}
