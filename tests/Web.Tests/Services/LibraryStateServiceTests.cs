using AwesomeAssertions;
using Web.Enums;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public class LibraryStateServiceTests
{
    private readonly LibraryStateService _service = new();

    [Fact]
    public void Load_Should_ReturnNull_WhenNoStateHasBeenSaved()
    {
        var result = _service.Load(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void Save_Should_PersistState_WhenCalled()
    {
        var libraryId = Guid.NewGuid();
        var state = new LibraryPageState("batman", ViewMode.Cards);

        _service.Save(libraryId, state);
        var result = _service.Load(libraryId);

        result.Should().NotBeNull();
        result!.Search.Should().Be("batman");
        result.View.Should().Be(ViewMode.Cards);
    }

    [Fact]
    public void Save_Should_OverwriteExistingState_WhenCalledTwiceForSameLibrary()
    {
        var libraryId = Guid.NewGuid();
        _service.Save(libraryId, new LibraryPageState("old", ViewMode.List));

        _service.Save(libraryId, new LibraryPageState("new", ViewMode.Covers));
        var result = _service.Load(libraryId);

        result!.Search.Should().Be("new");
        result.View.Should().Be(ViewMode.Covers);
    }

    [Fact]
    public void Load_Should_ReturnNull_ForUnknownLibrary_WhenOtherLibraryIsSaved()
    {
        var savedId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        _service.Save(savedId, new LibraryPageState("x", ViewMode.Cards));

        var result = _service.Load(otherId);

        result.Should().BeNull();
    }

    [Fact]
    public void Save_Should_StoreStatesIndependently_ForDifferentLibraries()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        _service.Save(id1, new LibraryPageState("search1", ViewMode.Cards));
        _service.Save(id2, new LibraryPageState("search2", ViewMode.List));

        _service.Load(id1)!.Search.Should().Be("search1");
        _service.Load(id2)!.Search.Should().Be("search2");
    }

    [Fact]
    public void Save_Should_AllowEmptySearchTerm()
    {
        var libraryId = Guid.NewGuid();
        _service.Save(libraryId, new LibraryPageState(string.Empty, ViewMode.Covers));

        var result = _service.Load(libraryId);

        result!.Search.Should().BeEmpty();
        result.View.Should().Be(ViewMode.Covers);
    }
}
