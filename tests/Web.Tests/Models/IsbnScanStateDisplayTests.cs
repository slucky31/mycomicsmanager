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
    [InlineData(IsbnScanState.Candidates, "Several ISBNs found in the pages: pick the book's own")]
    [InlineData(IsbnScanState.NotFound, "No ISBN found in the pages")]
    [InlineData(IsbnScanState.HasIsbn, "")]
    public void Label_Should_DescribeTheState(IsbnScanState state, string expected)
    {
        IsbnScanStateDisplay.Label(state).Should().Be(expected);
    }

    [Fact]
    public void Icon_Should_DifferForEachStateToShow()
    {
        IsbnScanState[] shown = [IsbnScanState.NotScanned, IsbnScanState.Candidates, IsbnScanState.NotFound];

        shown.Select(IsbnScanStateDisplay.Icon).Should().OnlyHaveUniqueItems().And.NotContain(string.Empty);
        IsbnScanStateDisplay.Icon(IsbnScanState.HasIsbn).Should().BeEmpty();
    }

    [Theory]
    [InlineData(IsbnScanState.Candidates, Color.Warning)]
    [InlineData(IsbnScanState.NotScanned, Color.Default)]
    [InlineData(IsbnScanState.NotFound, Color.Default)]
    public void Color_Should_OnlyHighlightTheCandidates(IsbnScanState state, Color expected)
    {
        IsbnScanStateDisplay.Color(state).Should().Be(expected);
    }
}
