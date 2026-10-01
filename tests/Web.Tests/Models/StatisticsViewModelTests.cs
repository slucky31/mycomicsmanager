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

    // ── From ──────────────────────────────────────────────────────────────────

    [Fact]
    public void From_Should_MapKeyFiguresAndChart_WhenAllLibrariesSelected()
    {
        var viewModel = StatisticsViewModel.From(CreateDto(), allLibraries: true);

        viewModel.HasBooks.Should().BeTrue();
        viewModel.KeyFigures.Select(c => (c.Label, c.Value)).Should().Equal(
            ("Books", "1,234"),
            ("Series", "12"),
            ("Libraries", "3"),
            ("Read", "1,000"),
            ("Unread", "234"),
            ("Readings", "1,500"),
            ("Average rating", "4.3 / 5"),
            ("Pages", "56,789"));
        viewModel.MonthLabels.Should().Equal("Dec 25", "Jan 26");
        viewModel.ReadingsSeries.Should().ContainSingle();
        viewModel.ReadingsSeries[0].Data.Values.Should().Equal(2d, 3d);
        viewModel.ReadingsInPeriod.Should().Be(5);
    }

    [Fact]
    public void From_Should_HideLibrariesAndShowStorage_WhenOneDigitalLibrarySelected()
    {
        var viewModel = StatisticsViewModel.From(CreateDto(digitalStorageBytes: 3 * 1024 * 1024), allLibraries: false);

        viewModel.KeyFigures.Should().NotContain(c => c.Label == "Libraries");
        viewModel.KeyFigures.Should().ContainSingle(c => c.Label == "Digital storage" && c.Value == "3 MB");
    }

    [Fact]
    public void From_Should_ShowDashRating_WhenNoBook()
    {
        var viewModel = StatisticsViewModel.From(new StatisticsDto(), allLibraries: true);

        viewModel.HasBooks.Should().BeFalse();
        viewModel.KeyFigures.Should().ContainSingle(c => c.Label == "Average rating" && c.Value == "–");
    }

    // ── FormatBytes ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(1023L, "1023 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(5L * 1024 * 1024 * 1024, "5 GB")]
    [InlineData(2L * 1024 * 1024 * 1024 * 1024 * 1024, "2048 TB")]
    public void FormatBytes_Should_UseLargestUnit_WhenBytesGiven(long bytes, string expected)
    {
        StatisticsViewModel.FormatBytes(bytes).Should().Be(expected);
    }
}
