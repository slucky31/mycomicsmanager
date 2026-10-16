using Application.Books.MoveTargets;
using AwesomeAssertions;
using Domain.Libraries;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookMoveChoicesTests
{
    private static readonly Guid s_current = Guid.CreateVersion7();
    private static readonly Guid s_bd = Guid.CreateVersion7();
    private static readonly Guid s_mangas = Guid.CreateVersion7();
    private static readonly Guid s_other = Guid.CreateVersion7();

    [Fact]
    public void From_Should_ListTheCurrentLibraryFirst_ThenTheTargets_WithTheirBooksAndTheSuggestion()
    {
        var targets = new BookMoveTargets(
            [
                new BookMoveTarget(s_bd, "BD", "#111111", "Bookmark", 3, 112),
                new BookMoveTarget(s_mangas, "Mangas", "#222222", "Star", 1, 1),
                new BookMoveTarget(s_other, "Divers", "#333333", "Folder", 0, 0),
            ],
            s_bd)
        {
            Current = new BookMoveTarget(s_current, "À trier", "#444444", "Archive", 0, 9),
            BookType = LibraryBookType.Digital,
        };

        var choices = BookMoveChoices.From(targets);

        choices.Libraries.Select(l => (l.Name, l.Icon, l.Details, l.IsSuggested, l.IsCurrent)).Should().Equal(
            ("À trier", "Archive", "Digital · current library", false, true),
            ("BD", "Bookmark", "Digital · 112 books · 3 of this series", true, false),
            ("Mangas", "Star", "Digital · 1 book · 1 of this series", false, false),
            ("Divers", "Folder", "Digital · No books yet", false, false));
        choices.CurrentName.Should().Be("À trier");
        choices.HasTargets.Should().BeTrue();
        choices.Preselected.Should().Be(s_bd);
        choices.Libraries[1].MoveLabel.Should().Be("Move to BD");
    }

    [Fact]
    public void From_Should_HaveNoTarget_AndNothingPreselected_WhenOnlyTheCurrentLibraryHasTheType()
    {
        var choices = BookMoveChoices.From(new BookMoveTargets([], null) { Current = new BookMoveTarget(s_current, "Papier", "#444444", "Archive", 0) });

        choices.Libraries.Should().ContainSingle().Which.Details.Should().Be("Physical · current library");
        choices.HasTargets.Should().BeFalse();
        choices.Preselected.Should().BeNull();
    }

    [Fact]
    public void From_Should_NameNoCurrentLibrary_WhenItIsUnknown()
    {
        var choices = BookMoveChoices.From(new BookMoveTargets([new BookMoveTarget(s_bd, "BD", "#111111", "Bookmark", 0, 2)], null));

        choices.CurrentName.Should().BeNull();
        choices.Libraries.Should().ContainSingle().Which.Details.Should().Be("Physical · 2 books");
    }
}
