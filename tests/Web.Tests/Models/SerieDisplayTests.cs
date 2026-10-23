using AwesomeAssertions;
using Domain.Series;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class SerieDisplayTests
{
    [Theory]
    [InlineData(4, 4, "#4")]
    [InlineData(16, 19, "#16–19")]
    public void RangeLabel_Should_NumberTheVolumesAsOnTheCovers(int first, int last, string expected)
    {
        SerieDisplay.RangeLabel(new VolumeRange(first, last)).Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 3, "Vol. 3")]
    [InlineData(1, 12, "Vol. 1–12")]
    public void VolumesLabel_Should_ShowTheFirstAndLastVolumes(int first, int last, string expected)
    {
        SerieDisplay.VolumesLabel(first, last).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "1 series", "1 volume")]
    [InlineData(4, "4 series", "4 volumes")]
    public void Counts_Should_AgreeWithTheNumber(int count, string series, string volumes)
    {
        SerieDisplay.SeriesCount(count).Should().Be(series);
        SerieDisplay.VolumeCount(count).Should().Be(volumes);
    }

    [Fact]
    public void SerieHref_Should_EscapeTheNameInTheQuery()
    {
        var libraryId = Guid.Parse("0193f3a8-0000-7000-8000-000000000001");

        SerieDisplay.SerieHref(libraryId, "Tintin & Milou / 2")
            .Should().Be("/libraries/0193f3a8-0000-7000-8000-000000000001/serie?name=Tintin%20%26%20Milou%20%2F%202");
        SerieDisplay.SeriesHref(libraryId).Should().Be("/libraries/0193f3a8-0000-7000-8000-000000000001/series");
    }
}
