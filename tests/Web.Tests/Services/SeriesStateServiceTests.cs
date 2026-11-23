using AwesomeAssertions;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class SeriesStateServiceTests
{
    private readonly SeriesStateService _service = new();

    [Fact]
    public void LoadSeries_Should_ReturnTheStateSaved_ForItsLibraryOnly()
    {
        var libraryId = Guid.NewGuid();
        _service.SaveSeries(libraryId, new SeriesPageState(SeriesFilter.Incomplete, 12));

        _service.LoadSeries(libraryId).Should().Be(new SeriesPageState(SeriesFilter.Incomplete, 12));
        _service.LoadSeries(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void LoadFirstVolume_Should_ReturnThePlaceSaved_ForItsSeriesOnly()
    {
        var libraryId = Guid.NewGuid();
        _service.SaveFirstVolume(libraryId, "Thorgal", 9);

        _service.LoadFirstVolume(libraryId, "Thorgal").Should().Be(9);
        _service.LoadFirstVolume(libraryId, "Blacksad").Should().Be(0);
        _service.LoadFirstVolume(Guid.NewGuid(), "Thorgal").Should().Be(0);
    }
}
