using Application.Interfaces;
using AwesomeAssertions;
using MudBlazor;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class ProviderSearchStateDisplayTests
{
    private static BnfBookResult BnfResult(bool found, bool failed = false) =>
        new(string.Empty, null, [], [], null, null, null, found, failed);

    private static BedethequeBookResult BedethequeResult(bool found, bool failed = false) =>
        new(string.Empty, string.Empty, 1, [], [], null, null, null, found, failed);

    [Fact]
    public void Of_Should_TellFoundFromNotFoundAndFailed()
    {
        ProviderSearchStates.Of(BnfResult(found: true)).Should().Be(ProviderSearchState.Found);
        ProviderSearchStates.Of(BnfResult(found: false)).Should().Be(ProviderSearchState.NotFound);
        ProviderSearchStates.Of(BnfResult(found: false, failed: true)).Should().Be(ProviderSearchState.Failed);
        ProviderSearchStates.Of((IBookSearchResult?)null).Should().Be(ProviderSearchState.Failed);
    }

    [Fact]
    public void Of_Should_ReadTheBedethequeResultTheSameWay()
    {
        ProviderSearchStates.Of(BedethequeResult(found: true)).Should().Be(ProviderSearchState.Found);
        ProviderSearchStates.Of(BedethequeResult(found: false)).Should().Be(ProviderSearchState.NotFound);
        ProviderSearchStates.Of(BedethequeResult(found: false, failed: true)).Should().Be(ProviderSearchState.Failed);
        ProviderSearchStates.Of((BedethequeBookResult?)null).Should().Be(ProviderSearchState.Failed);
    }

    [Fact]
    public void Icon_Should_DifferForEachState()
    {
        Enum.GetValues<ProviderSearchState>().Select(ProviderSearchStateDisplay.Icon)
            .Should().OnlyHaveUniqueItems().And.NotContain(string.Empty);
    }

    [Theory]
    [InlineData(ProviderSearchState.Searching, Color.Info)]
    [InlineData(ProviderSearchState.Found, Color.Success)]
    [InlineData(ProviderSearchState.NotFound, Color.Default)]
    [InlineData(ProviderSearchState.Failed, Color.Warning)]
    public void Color_Should_HighlightTheFoundAndFailedSources(ProviderSearchState state, Color expected)
    {
        ProviderSearchStateDisplay.Color(state).Should().Be(expected);
    }

    [Theory]
    [InlineData(ProviderSearchState.Searching, "BNF: searching…")]
    [InlineData(ProviderSearchState.Found, "BNF: book found")]
    [InlineData(ProviderSearchState.NotFound, "BNF: book not found")]
    [InlineData(ProviderSearchState.Failed, "BNF: could not be searched (error, timeout or blocked)")]
    public void Label_Should_DescribeTheState(ProviderSearchState state, string expected)
    {
        ProviderSearchStateDisplay.Label(state, "BNF").Should().Be(expected);
    }
}
