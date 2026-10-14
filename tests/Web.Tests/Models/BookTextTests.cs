using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookTextTests
{
    [Fact]
    public void Join_Should_ReturnNull_WhenNoPartIsKnown()
    {
        BookText.Join(null, " ", "").Should().BeNull();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(1, "1 page")]
    [InlineData(48, "48 pages")]
    public void PagesLabel_Should_CountThePages(int? pages, string? expected)
    {
        BookText.PagesLabel(pages).Should().Be(expected);
    }

    [Theory]
    [InlineData("Blacksad", "Âme rouge", 4, "Blacksad · Volume 4")]
    [InlineData(" extremity ", "Extremity", 1, "Volume 1")]
    [InlineData("", "Extremity", 0, null)]
    public void SerieAndVolume_Should_LeaveOutTheSeries_WhenItIsTheTitle(string serie, string title, int volume, string? expected)
    {
        BookText.SerieAndVolume(serie, title, volume).Should().Be(expected);
    }
}
