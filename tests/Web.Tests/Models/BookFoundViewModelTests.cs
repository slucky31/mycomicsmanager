using Application.Interfaces;
using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookFoundViewModelTests
{
    private static readonly ComicSearchResult Extremity = new(
        "Extremity", "Extremity", "9782413015406", 1, "/covers/extremity.webp",
        "Daniel Warren Johnson", "Delcourt", new DateOnly(2019, 4, 3), 271, Found: true)
    {
        Source = "BnF",
    };

    [Fact]
    public void From_Should_DescribeTheBookFound()
    {
        var view = BookFoundViewModel.From(Extremity);

        view.Should().BeEquivalentTo(new
        {
            Title = "Extremity",
            ImageLink = "/covers/extremity.webp",
            SerieAndVolume = "Volume 1",
            Authors = "Daniel Warren Johnson",
            Publication = "Delcourt · 2019 · 271 pages",
            IsbnLabel = "ISBN 9782413015406",
            SourceLabel = "Found on BnF",
            CoverAlt = "Cover of Extremity",
        });
    }

    [Fact]
    public void From_Should_ShowTheSeries_WhenItIsNotTheTitle()
    {
        var view = BookFoundViewModel.From(Extremity with { Title = "Artist", Serie = "Extremity", VolumeNumber = 2 });

        view.SerieAndVolume.Should().Be("Extremity · Volume 2");
    }

    [Fact]
    public void From_Should_LeaveOutWhatIsUnknown()
    {
        var view = BookFoundViewModel.From(Extremity with { Authors = " ", Publishers = "", PublishDate = null, NumberOfPages = null, Source = null });

        view.Authors.Should().BeNull();
        view.Publication.Should().BeNull();
        view.SourceLabel.Should().BeNull();
    }
}
