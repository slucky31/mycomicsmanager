using AwesomeAssertions;
using Domain.Series;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class SerieCardViewModelTests
{
    [Fact]
    public void From_Should_ShowWhatIsMissing_WhenTheSeriesIsIncomplete()
    {
        var card = SerieCardViewModel.From(SeriesTestData.Summary("Les Vieux Fourneaux", [1, 2, 3, 5, 6, 8], cover: "https://c/1.jpg"));

        card.Should().BeEquivalentTo(new
        {
            Name = "Les Vieux Fourneaux",
            Cover = "https://c/1.jpg",
            CountLabel = "6 vols",
            Detail = "Vol. 1–8 · 2 missing",
            Badge = "2 missing",
            BadgeKind = SerieBadgeKind.Missing,
            Status = SerieCompletenessStatus.Incomplete,
        });
        card.Href.Should().Be(SerieDisplay.SerieHref(SeriesTestData.LibraryId, "Les Vieux Fourneaux"));
    }

    [Fact]
    public void From_Should_SayComplete_WhenEveryPublishedVolumeIsOwned()
    {
        var card = SerieCardViewModel.From(SeriesTestData.Summary("Blacksad", [1, 2, 3], total: 3));

        card.Detail.Should().Be("Vol. 1–3 · complete");
        card.Badge.Should().Be("Complete");
        card.BadgeKind.Should().Be(SerieBadgeKind.Complete);
    }

    [Fact]
    public void From_Should_ShowNoBadge_WhenTheTotalIsUnknown()
    {
        var card = SerieCardViewModel.From(SeriesTestData.Summary("Astérix", [1]));

        card.CountLabel.Should().Be("1 vol");
        card.Detail.Should().Be("Vol. 1");
        card.Badge.Should().BeNull();
        card.BadgeKind.Should().Be(SerieBadgeKind.None);
    }

    [Fact]
    public void From_Should_CountBooks_ForTheOneShots()
    {
        var card = SerieCardViewModel.From(SeriesTestData.Summary("One Shot", [1, 1, 1]));

        card.CountLabel.Should().Be("3 books");
        card.Detail.Should().Be("Single volumes");
        card.Badge.Should().BeNull();
    }

    [Theory]
    [InlineData(SeriesFilter.All, true, true, true)]
    [InlineData(SeriesFilter.Incomplete, true, false, false)]
    [InlineData(SeriesFilter.Complete, false, true, false)]
    public void Matches_Should_KeepTheSeriesOfTheFilter(SeriesFilter filter, bool incomplete, bool complete, bool unknown)
    {
        SerieCardViewModel.From(SeriesTestData.Summary("A", [2])).Matches(filter).Should().Be(incomplete);
        SerieCardViewModel.From(SeriesTestData.Summary("B", [1], total: 1)).Matches(filter).Should().Be(complete);
        SerieCardViewModel.From(SeriesTestData.Summary("C", [1])).Matches(filter).Should().Be(unknown);
    }
}
