using AwesomeAssertions;
using Domain.Libraries;
using Web.Enums;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class LibraryDisplayTests
{
    [Theory]
    [InlineData(0, "0 books")]
    [InlineData(1, "1 book")]
    [InlineData(24, "24 books")]
    public void BookCount_Should_AgreeWithTheNumberOfBooks(int count, string expected) =>
        LibraryDisplay.BookCount(count).Should().Be(expected);

    [Theory]
    [InlineData(BookSortOrder.IdDesc, "Newest first")]
    [InlineData(BookSortOrder.IdAsc, "Oldest first")]
    [InlineData(BookSortOrder.SerieAndVolumeAsc, "Series & volume")]
    public void SortLabel_Should_NameTheSortOrder(BookSortOrder sortOrder, string expected) =>
        LibraryDisplay.SortLabel(sortOrder).Should().Be(expected);

    [Theory]
    [InlineData(ViewMode.Covers, "Covers")]
    [InlineData(ViewMode.List, "List")]
    public void ViewLabel_Should_NameTheView(ViewMode viewMode, string expected) =>
        LibraryDisplay.ViewLabel(viewMode).Should().Be(expected);

    [Fact]
    public void Subtitle_Should_GiveTheNumberOfBooksAndTheSortOrder() =>
        LibraryDisplay.Subtitle(24, BookSortOrder.IdDesc).Should().Be("24 books · Newest first");

    [Fact]
    public void Subtitle_Should_GiveTheSortOrderAlone_WhenTheNumberOfBooksIsUnknown() =>
        LibraryDisplay.Subtitle(null, BookSortOrder.SerieAndVolumeAsc).Should().Be("Series & volume");

    [Theory]
    [InlineData(1, "1 book matches “hiver”")]
    [InlineData(2, "2 books match “hiver”")]
    public void SearchResults_Should_TellHowManyBooksMatch(int count, string expected) =>
        LibraryDisplay.SearchResults(count, "hiver").Should().Be(expected);
}
