using Application.Books.MoveTargets;
using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookMoveTargetViewModelTests
{
    [Fact]
    public void From_Should_DescribeSameSerieCountAndFlagSuggestedLibrary()
    {
        var bd = Guid.CreateVersion7();
        var mangas = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        var targets = new BookMoveTargets(
            [new BookMoveTarget(bd, "BD", "#111111", "Bookmark", 3), new BookMoveTarget(mangas, "Mangas", "#222222", "Bookmark", 1), new BookMoveTarget(other, "Divers", "#333333", "Bookmark", 0)],
            bd);

        var options = BookMoveTargetViewModel.From(targets);

        options.Select(o => o.Hint).Should().Equal("3 books of this series", "1 book of this series", null);
        options.Select(o => o.IsSuggested).Should().Equal(true, false, false);
    }
}
