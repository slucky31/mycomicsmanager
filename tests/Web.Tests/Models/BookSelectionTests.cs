using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class BookSelectionTests
{
    private static BookListItemViewModel Book(Guid id) => new(id, "Blacksad", "Âme rouge", null, 1, "", "", "", null, null, 0);

    [Fact]
    public void None_Should_CheckNothing_AndStayInactive()
    {
        var id = Guid.CreateVersion7();

        var selection = BookSelection.None.Toggle(id).With([id]);

        selection.Should().BeSameAs(BookSelection.None);
        selection.IsActive.Should().BeFalse();
        selection.IsChecked(Book(id)).Should().BeFalse();
        selection.AriaChecked(Book(id)).Should().Be("false");
    }

    [Fact]
    public void Toggle_Should_CheckThenUncheckABook_AndWithShouldAddTheOthersOnce()
    {
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        var selection = BookSelection.Start(first);
        selection.Label.Should().Be("1 selected");
        selection.Toggle(first).Label.Should().Be("Choose books");

        var all = selection.With([first, second]);
        all.Count.Should().Be(2);
        all.BookIds.Should().BeEquivalentTo([first, second]);
        all.Label.Should().Be("2 selected");
        all.IsChecked(Book(second)).Should().BeTrue();
        BookSelection.Start().Count.Should().Be(0);
    }
}
