using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class LibraryColorsTests
{
    [Fact]
    public void Palette_Should_NameTwelveDistinctColors()
    {
        LibraryColors.Palette.Should().HaveCount(12);
        LibraryColors.Palette.Select(c => c.Hex).Should().OnlyHaveUniqueItems();
        LibraryColors.Palette.Should().OnlyContain(c => c.Name.Length > 0);
    }

    [Theory]
    [InlineData("#26A69A", true)]
    [InlineData("#26a69a", true)]
    [InlineData("#5C6BC0", false)]
    [InlineData(null, false)]
    public void IsSelected_Should_CompareTheHexIgnoringCase(string? hex, bool expected) =>
        LibraryColors.IsSelected(new LibraryColor("#26A69A", "Teal"), hex).Should().Be(expected);
}
