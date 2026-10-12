using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class ReaderDisplayTests
{
    [Theory]
    [InlineData("Blacksad", "Arctic Nation", "Blacksad · Arctic Nation")]
    [InlineData("Blacksad", "Blacksad - Arctic Nation", "Blacksad - Arctic Nation")]
    [InlineData("blacksad", "Blacksad", "Blacksad")]
    [InlineData(" ", "Arctic Nation", "Arctic Nation")]
    [InlineData(null, "Arctic Nation", "Arctic Nation")]
    [InlineData("Blacksad", "  ", "Blacksad")]
    [InlineData("Blacksad", null, "Blacksad")]
    public void Title_Should_NotRepeatTheSerie(string? serie, string? title, string expected)
    {
        ReaderDisplay.Title(serie, title).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 4, "25%")]
    [InlineData(4, 188, "2.66%")]
    [InlineData(187, 188, "100%")]
    [InlineData(250, 188, "100%")]
    [InlineData(0, 0, "0%")]
    public void ProgressWidth_Should_ReturnTheShareOfTheBookRead(int pageIndex, int pageCount, string expected)
    {
        ReaderDisplay.ProgressWidth(pageIndex, pageCount).Should().Be(expected);
    }
}
