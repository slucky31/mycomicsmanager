using Application.Libraries.Summaries;
using AwesomeAssertions;
using Domain.Libraries;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class NavLibraryItemViewModelTests
{
    [Fact]
    public void From_Should_LinkToTheLibraryAndDescribeItsBooks()
    {
        var id = Guid.CreateVersion7();
        var summary = new LibrarySummary(id, "Read Books", "#2E7D32", "Star", LibraryBookType.Physical, 24);

        var item = NavLibraryItemViewModel.From(summary);

        item.Id.Should().Be(id);
        item.Name.Should().Be("Read Books");
        item.Color.Should().Be("#2E7D32");
        item.Icon.Should().Be("Star");
        item.Href.Should().Be($"/libraries/{id}");
        item.CountLabel.Should().Be("24");
        item.AriaLabel.Should().Be("Read Books, 24 books");
        item.CssClass.Should().Be("side-nav-library");
    }

    [Fact]
    public void From_Should_DimAndNameAHiddenLibrary()
    {
        var summary = new LibrarySummary(Guid.CreateVersion7(), "Loans", "#5C6BC0", "Bookmark", LibraryBookType.Physical, 3) { IsHidden = true };

        var item = NavLibraryItemViewModel.From(summary);

        item.CssClass.Should().Be("side-nav-library side-nav-library-hidden");
        item.AriaLabel.Should().Be("Loans, hidden, 3 books");
    }

    [Fact]
    public void From_Should_UseTheSingular_WhenTheLibraryHasOneBook()
    {
        var summary = new LibrarySummary(Guid.CreateVersion7(), "MesEbooks", "#5C6BC0", "Bookmark", LibraryBookType.Digital, 1);

        NavLibraryItemViewModel.From(summary).AriaLabel.Should().Be("MesEbooks, 1 book");
    }
}
