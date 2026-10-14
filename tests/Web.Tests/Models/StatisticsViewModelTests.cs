using Application.Statistics.Get;
using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class StatisticsViewModelTests
{
    private static StatisticsDto CreateDto(long digitalStorageBytes = 0) => new()
    {
        TotalBooks = 1234,
        TotalSeries = 12,
        TotalLibraries = 3,
        ReadBooks = 1000,
        TotalReadings = 1500,
        AverageRating = 4.25,
        TotalPages = 56789,
        DigitalStorageBytes = digitalStorageBytes,
        ReadingsPerMonth =
        [
            new MonthlyReadingCountDto(2025, 12, 2),
            new MonthlyReadingCountDto(2026, 1, 3)
        ]
    };

    private static List<MonthlyReadingCountDto> Months(params int[] counts) =>
        counts.Select((count, index) => new MonthlyReadingCountDto(2025, index + 1, count)).ToList();

    // ── StatisticsViewModel.From ──────────────────────────────────────────────

    [Fact]
    public void From_Should_MapHeadlineAndTiles_WhenAllLibrariesSelected()
    {
        var viewModel = StatisticsViewModel.From(CreateDto(), allLibraries: true);

        viewModel.HasBooks.Should().BeTrue();
        viewModel.TotalBooks.Should().Be("1,234");
        viewModel.BooksUnit.Should().Be("books");
        viewModel.ReadBooks.Should().Be("1,000");
        viewModel.UnreadBooks.Should().Be("234");
        viewModel.ReadPercent.Should().Be(81);
        viewModel.Tiles.Select(t => (t.Label, t.Value, t.Suffix)).Should().Equal(
            ("Series", "12", null),
            ("Readings", "1,500", null),
            ("Avg rating", "4.3", " / 5"),
            ("Pages", "56,789", null),
            ("Libraries", "3", null));
        viewModel.Chart.Total.Should().Be(5);
    }

    [Fact]
    public void From_Should_HideLibrariesAndShowStorage_WhenOneDigitalLibrarySelected()
    {
        var viewModel = StatisticsViewModel.From(CreateDto(digitalStorageBytes: 3 * 1024 * 1024), allLibraries: false);

        viewModel.Tiles.Should().NotContain(t => t.Label == "Libraries");
        viewModel.Tiles.Should().ContainSingle(t => t.Label == "Storage" && t.Value == "3 MB");
    }

    [Fact]
    public void From_Should_ShowDashRatingAndZeroPercent_WhenNoBook()
    {
        var viewModel = StatisticsViewModel.From(new StatisticsDto(), allLibraries: true);

        viewModel.HasBooks.Should().BeFalse();
        viewModel.ReadPercent.Should().Be(0);
        viewModel.Tiles.Should().ContainSingle(t => t.Label == "Avg rating" && t.Value == "–" && t.Suffix == null);
    }

    [Fact]
    public void From_Should_UseSingularAndRoundDown_WhenOneOfTwoBooksIsAlmostRead()
    {
        var single = StatisticsViewModel.From(new StatisticsDto { TotalBooks = 1 }, allLibraries: true);
        var almostAll = StatisticsViewModel.From(new StatisticsDto { TotalBooks = 200, ReadBooks = 199 }, allLibraries: true);

        single.BooksUnit.Should().Be("book");
        almostAll.ReadPercent.Should().Be(99);
    }

    // ── ReadingsChartViewModel.From ───────────────────────────────────────────

    [Fact]
    public void ChartFrom_Should_ScaleBarsAndMarkPeak_WhenMonthsHaveReadings()
    {
        var chart = ReadingsChartViewModel.From(Months(1, 6, 0, 35, 3));

        chart.AxisTicks.Should().Equal(0, 20, 40);
        chart.Total.Should().Be(45);
        chart.Bars.Select(b => b.Initial).Should().Equal("J", "F", "M", "A", "M");
        chart.Bars[3].Month.Should().Be("April 2025");
        chart.Bars.Select(b => b.HeightPercent).Should().Equal(2.5, 15, 0, 87.5, 7.5);
        chart.Bars.Select(b => b.IsPeak).Should().Equal(false, false, false, true, false);
        chart.AriaLabel.Should().Be("Readings per month over the last 5 months: 45 readings, peak of 35 in April");
    }

    [Fact]
    public void ChartFrom_Should_MarkMostRecentPeak_WhenSeveralMonthsShareIt()
    {
        var chart = ReadingsChartViewModel.From(Months(4, 1, 4, 2));

        chart.Bars.Select(b => b.IsPeak).Should().Equal(false, false, true, false);
    }

    [Fact]
    public void ChartFrom_Should_MarkFirstMonth_WhenItIsTheOnlyPeak()
    {
        var chart = ReadingsChartViewModel.From(Months(1, 0));

        chart.Bars.Select(b => b.IsPeak).Should().Equal(true, false);
        chart.AriaLabel.Should().Be("Readings per month over the last 2 months: 1 reading, peak of 1 in January");
    }

    [Fact]
    public void ChartFrom_Should_HaveNoPeak_WhenNoReading()
    {
        var chart = ReadingsChartViewModel.From(Months(0, 0));

        chart.AxisTicks.Should().Equal(0, 1, 2);
        chart.Bars.Should().NotContain(b => b.IsPeak);
        chart.AriaLabel.Should().Be("Readings per month over the last 2 months: no readings");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(10, 5)]
    [InlineData(11, 10)]
    [InlineData(35, 20)]
    [InlineData(41, 50)]
    [InlineData(250, 200)]
    public void AxisStep_Should_ReturnSmallestRoundStep_WhenTwoStepsReachPeak(int peak, int expected)
    {
        ReadingsChartViewModel.AxisStep(peak).Should().Be(expected);
    }
}
