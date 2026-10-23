using Application.Series.Missing;
using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class MissingVolumesViewModelTests
{
    private static readonly Guid s_otherLibraryId = Guid.CreateVersion7();

    private static MissingVolumesViewModel Sample() => MissingVolumesViewModel.From(
    [
        new LibraryMissingVolumes(SeriesTestData.LibraryId, "BD",
        [
            SeriesTestData.Summary("Thorgal", [1, 2, 5], cover: "https://c/t.jpg"),
            SeriesTestData.Summary("XIII", [1], total: 4),
        ]),
        new LibraryMissingVolumes(s_otherLibraryId, "Comics", [SeriesTestData.Summary("Saga", [2])]),
    ]);

    [Fact]
    public void From_Should_ListTheMissingVolumesOfEachSeries()
    {
        var missing = Sample();

        missing.Subtitle.Should().Be("6 volumes in 3 series");
        var thorgal = missing.Libraries[0].Series[0];
        thorgal.Should().BeEquivalentTo(new
        {
            Name = "Thorgal",
            Cover = "https://c/t.jpg",
            Href = SerieDisplay.SerieHref(SeriesTestData.LibraryId, "Thorgal"),
            OwnedLabel = "3 / 5",
        });
        thorgal.Missing.Should().Equal("#3–4");
        missing.Libraries[0].Series[1].Missing.Should().Equal("#2–4");
    }

    [Fact]
    public void Subtitle_Should_SayNothingIsMissing_WhenThereIsNoLibrary()
    {
        MissingVolumesViewModel.From([]).Subtitle.Should().Be("Nothing missing");
    }

    [Fact]
    public void In_Should_KeepTheChosenLibrary_OrEveryOneWhenNone()
    {
        var missing = Sample();

        missing.In(Guid.Empty).Select(l => l.Name).Should().Equal("BD", "Comics");
        missing.In(s_otherLibraryId).Select(l => l.Name).Should().Equal("Comics");
    }
}
