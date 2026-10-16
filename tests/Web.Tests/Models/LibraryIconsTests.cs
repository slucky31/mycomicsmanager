using AwesomeAssertions;
using MudBlazor;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class LibraryIconsTests
{
    [Fact]
    public void Suggested_Should_OnlyHoldMaterialIcons()
    {
        LibraryIcons.Suggested.Should().OnlyContain(choice => LibraryIcons.Exists(choice.Name));
        LibraryIcons.Suggested.Select(choice => choice.Name).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Resolve_Should_GiveTheSvgOfTheIcon_OrAStar_WhenTheNameIsUnknown()
    {
        LibraryIcons.Resolve("MenuBook").Should().Be(Icons.Material.Filled.MenuBook);
        LibraryIcons.Resolve("NoSuchIcon").Should().Be(Icons.Material.Filled.Star);
        LibraryIcons.Resolve(null).Should().Be(Icons.Material.Filled.Star);
    }

    [Theory]
    [InlineData("Favorite", "Heart")]
    [InlineData("AutoStories", "Auto stories")]
    [InlineData("Filter9Plus", "Filter 9 plus")]
    public void LabelOf_Should_NameTheSuggestion_OrSplitTheMaterialName(string name, string expected) =>
        LibraryIcons.LabelOf(name).Should().Be(expected);

    [Fact]
    public void Search_Should_GiveTheSuggestions_WithTheChosenIconFirst_WhenItIsNotOneOfThem()
    {
        LibraryIcons.Search(null, "AutoStories").Select(c => c.Name).Should()
            .Equal(["AutoStories", .. LibraryIcons.Suggested.Select(c => c.Name)]);
        LibraryIcons.Search(" ", "Star").Should().Equal(LibraryIcons.Suggested);
        LibraryIcons.Search("", "NoSuchIcon").Should().Equal(LibraryIcons.Suggested);
    }

    [Fact]
    public void Search_Should_FindTheIconsWithAllTheWordsSearched_InTheirNameOrLabel()
    {
        LibraryIcons.Search("auto stor", null).Select(c => c.Name).Should().Contain("AutoStories")
            .And.OnlyContain(name => name.Contains("Auto", StringComparison.OrdinalIgnoreCase));
        LibraryIcons.Search("heart", null).Select(c => c.Name).Should().Contain("Favorite");
        LibraryIcons.Search("a", null).Should().HaveCount(LibraryIcons.MaxResults);
        LibraryIcons.Search("zzzz", null).Should().BeEmpty();
    }

    [Theory]
    [InlineData("", 16, "Suggested")]
    [InlineData("book", 40, "First 40 icons")]
    [InlineData("book", 1, "1 icon")]
    [InlineData("book", 7, "7 icons")]
    public void ResultsCaption_Should_SayWhatTheGridShows(string query, int count, string expected) =>
        LibraryIcons.ResultsCaption(query, count).Should().Be(expected);

    [Fact]
    public void Svg_Should_BeTheSvgOfTheIcon() =>
        LibraryIcons.ChoiceOf("Folder").Svg.Should().Be(Icons.Material.Filled.Folder);
}
