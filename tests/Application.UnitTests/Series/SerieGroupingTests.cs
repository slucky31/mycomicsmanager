using Application.Series;
using Domain.Series;

namespace Application.UnitTests.Series;

public class SerieGroupingTests
{
    private static readonly Guid s_libraryId = Guid.CreateVersion7();
    private static readonly Guid s_otherLibraryId = Guid.CreateVersion7();

    [Fact]
    public void Summarize_Should_GroupTheBooksByNameWhateverTheCase_InEachLibrary()
    {
        SerieBookRow[] books =
        [
            SerieRows.Book(s_libraryId, "Thorgal", 2, "https://covers/t2.jpg"),
            SerieRows.Book(s_libraryId, " thorgal", 1),
            SerieRows.Book(s_libraryId, "Blacksad", 1, "https://covers/b1.jpg"),
            SerieRows.Book(s_otherLibraryId, "Thorgal", 1),
        ];

        var series = SerieGrouping.Summarize(books, []);

        series.Select(s => (s.Name, s.LibraryId, s.BookCount)).Should().HaveCount(3)
            .And.Contain(("Blacksad", s_libraryId, 1))
            .And.Contain(("thorgal", s_libraryId, 2))
            .And.Contain(("Thorgal", s_otherLibraryId, 1));
        series[0].Name.Should().Be("Blacksad");
        var thorgal = series.Single(s => s.LibraryId == s_libraryId && s.BookCount == 2);
        thorgal.Cover.Should().Be("https://covers/t2.jpg");
        thorgal.FirstVolume.Should().Be(1);
        thorgal.LastVolume.Should().Be(2);
    }

    [Fact]
    public void Summarize_Should_UseTheTotalTheUserGave()
    {
        var info = SeriesInfo.Create(s_libraryId, "THORGAL", 4, isFinished: true).Value!;

        var serie = SerieGrouping.Summarize([SerieRows.Book(s_libraryId, "Thorgal", 1)], [info]).Single();

        serie.TotalVolumes.Should().Be(4);
        serie.IsFinished.Should().BeTrue();
        serie.Completeness.Missing.Should().Equal(new VolumeRange(2, 4));
    }

    [Fact]
    public void Summarize_Should_NotMeasureTheOneShots()
    {
        var serie = SerieGrouping.Summarize([SerieRows.Book(s_libraryId, "One Shot", 1), SerieRows.Book(s_libraryId, "One Shot", 1)], []).Single();

        serie.IsOneShot.Should().BeTrue();
        serie.BookCount.Should().Be(2);
        serie.Completeness.Should().Be(SerieCompleteness.NotApplicable);
    }

    [Fact]
    public void Summarize_Should_Throw_WhenASeriesHasNoBook()
    {
        var act = () => SerieGrouping.Summarize([], info: null);

        act.Should().Throw<ArgumentException>();
    }
}
