using Application.Books.Reading;
using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class ReadingPageViewModelTests
{
    private static BookInProgressDto InProgress(int lastReadPage, int? numberOfPages) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), "Blacksad", "Amarillo", 5, "cover.jpg", lastReadPage, numberOfPages, null);

    private static BookReadingViewModel Reading(DateTime date) =>
        BookReadingViewModel.From(new BookReadingDto(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Thorgal", "Le Fils", 12, "", date, 4));

    [Fact]
    public void From_Should_ShowWhereTheReaderStopped_WhenTheNumberOfPagesIsKnown()
    {
        var dto = InProgress(33, 56);

        var book = BookInProgressViewModel.From(dto);

        book.Title.Should().Be("Amarillo");
        book.SerieAndVolume.Should().Be("Blacksad · Volume 5");
        book.Progress.Should().Be("Page 34 of 56");
        book.Percent.Should().Be(60);
        book.ProgressStyle.Should().Be("width: 60%;");
        book.ReadHref.Should().Be($"/books/{dto.Id}/read");
        book.AriaLabel.Should().Be("Continue Amarillo, Page 34 of 56");
    }

    [Fact]
    public void From_Should_ShowThePageAlone_WhenTheNumberOfPagesIsUnknown()
    {
        var book = BookInProgressViewModel.From(InProgress(11, null));

        book.Progress.Should().Be("Page 12");
        book.Percent.Should().Be(0);
    }

    [Fact]
    public void From_Should_ShowTheDateAndRatingOfTheReading()
    {
        var reading = Reading(new DateTime(2026, 10, 27, 20, 0, 0, DateTimeKind.Utc));

        reading.DateLabel.Should().Be("27/10/2026");
        reading.RatingLabel.Should().Be("Rated 4 out of 5");
        reading.SerieAndVolume.Should().Be("Thorgal · Volume 12");
        reading.Href.Should().Be($"/books/{reading.BookId}");
    }

    [Fact]
    public void ByMonth_Should_GroupTheReadingsByMonth_KeepingTheirOrder()
    {
        var last = Reading(new DateTime(2026, 10, 27, 20, 0, 0, DateTimeKind.Utc));
        var middle = Reading(new DateTime(2026, 10, 2, 20, 0, 0, DateTimeKind.Utc));
        var first = Reading(new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc));

        var months = ReadingPageDisplay.ByMonth([last, middle, first]);

        months.Select(m => m.Label).Should().Equal("October 2026", "September 2026");
        months[0].Readings.Should().Equal(last, middle);
        months[1].Readings.Should().Equal(first);
    }

    [Theory]
    [InlineData(1, "Show 1 more reading")]
    [InlineData(20, "Show 20 more readings")]
    public void MoreLabel_Should_CountTheReadingsLeft(int remaining, string expected) =>
        ReadingPageDisplay.MoreLabel(remaining).Should().Be(expected);

    [Theory]
    [InlineData(0, 1, "0 in progress · 1 reading")]
    [InlineData(3, 48, "3 in progress · 48 readings")]
    public void Subtitle_Should_CountTheBooksInProgressAndTheReadings(int inProgress, int readings, string expected) =>
        ReadingPageDisplay.Subtitle(inProgress, readings).Should().Be(expected);
}
