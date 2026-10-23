using Application.Series;
using Application.Series.Get;
using AwesomeAssertions;
using Domain.Series;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class SeriePageViewModelTests
{
    private static SerieDetail Detail(string name, SerieBookRow[] books, int? total = null, bool isFinished = false) =>
        new("BD", SeriesTestData.Summary(name, books.Select(b => b.VolumeNumber).ToArray(), total, isFinished), books);

    [Fact]
    public void From_Should_PutTheMissingVolumesBetweenTheBooks()
    {
        var first = SeriesTestData.Book("Thorgal", 1, "https://c/1.jpg");
        var third = SeriesTestData.Book("Thorgal", 3);

        var page = SeriePageViewModel.From(Detail("Thorgal", [first, third], total: 6, isFinished: true));

        page.Name.Should().Be("Thorgal");
        page.Subtitle.Should().Be("in BD · 2 of 6 volumes");
        page.ShowsCompleteness.Should().BeTrue();
        page.ProgressLabel.Should().Be("4 volumes missing");
        page.ProgressCount.Should().Be("2 / 6");
        page.ProgressPercent.Should().Be(33);
        page.PublishedLabel.Should().Be("Volumes published: 6 · finished");
        page.TotalVolumes.Should().Be(6);
        page.IsFinished.Should().BeTrue();
        page.Slots.Select(s => (s.Badge, s.IsMissing)).Should().Equal(("#1", false), ("#2", true), ("#3", false), ("#4–6", true));
        page.Slots[0].Should().Be(new SerieSlotViewModel(first.BookId.ToString(), "#1", "Thorgal 1", "https://c/1.jpg", $"/books/{first.BookId}"));
        page.Slots[1].Key.Should().Be("missing-2");
    }

    [Theory]
    [InlineData(new[] { 1, 3 }, null, "1 volume missing", "Volumes published: unknown")]
    [InlineData(new[] { 1, 2 }, 2, "Complete", "Volumes published: 2 · ongoing")]
    [InlineData(new[] { 1, 2 }, null, "No gap, number of volumes published unknown", "Volumes published: unknown")]
    public void From_Should_SayHowCompleteTheSeriesIs(int[] volumes, int? total, string progress, string published)
    {
        var page = SeriePageViewModel.From(Detail("Blacksad", volumes.Select(v => SeriesTestData.Book("Blacksad", v)).ToArray(), total));

        page.ProgressLabel.Should().Be(progress);
        page.PublishedLabel.Should().Be(published);
    }

    [Fact]
    public void From_Should_HideTheCompleteness_OfTheOneShots()
    {
        var page = SeriePageViewModel.From(Detail("One Shot", [SeriesTestData.Book("One Shot", 1), SeriesTestData.Book("One Shot", 1)]));

        page.ShowsCompleteness.Should().BeFalse();
        page.Subtitle.Should().Be("in BD · 2 books");
        page.ProgressPercent.Should().Be(0);
        page.Slots.Should().HaveCount(2).And.OnlyContain(s => !s.IsMissing);
    }
}
