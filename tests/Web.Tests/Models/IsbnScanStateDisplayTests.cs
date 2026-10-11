using AwesomeAssertions;
using Domain.Books;
using MudBlazor;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class IsbnScanStateDisplayTests
{
    [Theory]
    [InlineData(IsbnScanState.NotScanned, "No ISBN: the pages were not scanned yet")]
    [InlineData(IsbnScanState.Candidates, "2 ISBNs found in the pages: pick the book's own")]
    [InlineData(IsbnScanState.NotFound, "No ISBN found in the pages")]
    [InlineData(IsbnScanState.HasIsbn, "")]
    public void Label_Should_DescribeTheState(IsbnScanState state, string expected)
    {
        IsbnScanStateDisplay.Label(state, candidateCount: 2).Should().Be(expected);
    }

    [Fact]
    public void Icon_Should_DifferForEachStateToShow()
    {
        IsbnScanState[] shown = [IsbnScanState.NotScanned, IsbnScanState.Candidates, IsbnScanState.NotFound];

        shown.Select(IsbnScanStateDisplay.Icon).Should().OnlyHaveUniqueItems().And.NotContain(string.Empty);
        IsbnScanStateDisplay.Icon(IsbnScanState.HasIsbn).Should().BeEmpty();
    }

    [Theory]
    [InlineData(IsbnScanState.NotScanned, "?")]
    [InlineData(IsbnScanState.Candidates, "3")]
    [InlineData(IsbnScanState.NotFound, null)]
    [InlineData(IsbnScanState.HasIsbn, null)]
    public void BadgeContent_Should_TellWhyTheIsbnIsMissing(IsbnScanState state, string? expected)
    {
        IsbnScanStateDisplay.BadgeContent(state, candidateCount: 3).Should().Be(expected);
    }

    [Fact]
    public void BadgeIcon_Should_OnlyBeACross_WhenNothingWasFound()
    {
        IsbnScanStateDisplay.BadgeIcon(IsbnScanState.NotFound).Should().Be(Icons.Material.Filled.Close);
        IsbnScanStateDisplay.BadgeIcon(IsbnScanState.NotScanned).Should().BeNull();
        IsbnScanStateDisplay.BadgeIcon(IsbnScanState.Candidates).Should().BeNull();
    }

    [Theory]
    [InlineData(IsbnScanState.Candidates, Color.Warning)]
    [InlineData(IsbnScanState.NotFound, Color.Error)]
    [InlineData(IsbnScanState.NotScanned, Color.Dark)]
    public void BadgeColor_Should_HighlightTheCandidates(IsbnScanState state, Color expected)
    {
        IsbnScanStateDisplay.BadgeColor(state).Should().Be(expected);
    }
}
