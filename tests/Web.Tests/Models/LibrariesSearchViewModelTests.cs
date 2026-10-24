using Application.Books.Search;
using AwesomeAssertions;
using Web.Models;
using Xunit;

namespace Web.Tests.Models;

public sealed class LibrariesSearchViewModelTests
{
    private static readonly LibraryCardViewModel s_comics = new(Guid.CreateVersion7(), "BD", "#E65100", "Star", false, 40, []);

    [Fact]
    public void From_Should_GiveEachResultItsLibraryAndLink()
    {
        var serie = new SerieSearchHit(s_comics.Id, "Astérix & Obélix", 38);
        var book = new BookSearchHit(Guid.CreateVersion7(), s_comics.Id, "La Serpe d'or", "Astérix", 2, "/covers/2.webp");

        var view = LibrariesSearchViewModel.From(new BookSearchResult([serie], [book], 1), [s_comics]);

        view.Series.Should().ContainSingle().Which.Should().Be(new SerieSearchItemViewModel(
            "Astérix & Obélix", new SearchLibrary("BD", "#E65100", "Star"), "38 volumes", $"/libraries/{s_comics.Id}/serie?name=Ast%C3%A9rix%20%26%20Ob%C3%A9lix"));
        var item = view.Books.Should().ContainSingle().Which;
        item.Should().Be(new BookSearchItemViewModel(book.Id, "La Serpe d'or", "Astérix · #2", "/covers/2.webp", new SearchLibrary("BD", "#E65100", "Star")));
        item.Href.Should().Be($"/books/{book.Id}");
        view.SeriesHeading.Should().Be("Series · 1");
        view.BooksHeading.Should().Be("Books · 1");
        view.HasResults.Should().BeTrue();
        view.RemainingBookCount.Should().Be(0);
    }

    [Fact]
    public void From_Should_LeaveTheLibraryBlank_WhenItIsNotKnown()
    {
        var book = new BookSearchHit(Guid.CreateVersion7(), Guid.CreateVersion7(), "Le Dernier Gaulois", "", 0, "");

        var view = LibrariesSearchViewModel.From(new BookSearchResult([], [book], 1), [s_comics]);

        view.Books[0].Library.Should().Be(SearchLibrary.Unknown);
        view.Books[0].SerieAndVolume.Should().BeEmpty();
    }

    [Theory]
    [InlineData(21, "Show 1 more book")]
    [InlineData(45, "Show 25 more books")]
    public void MoreLabel_Should_CountTheBooksNotShownYet(int bookCount, string expected)
    {
        var books = Enumerable.Range(1, 20)
            .Select(i => new BookSearchItemViewModel(Guid.CreateVersion7(), $"Book {i}", "", "", SearchLibrary.Unknown))
            .ToList();

        var view = new LibrariesSearchViewModel([], books, bookCount);

        view.MoreLabel.Should().Be(expected);
    }

    [Fact]
    public void Empty_Should_HaveNoResults()
    {
        LibrariesSearchViewModel.Empty.HasResults.Should().BeFalse();
    }
}
