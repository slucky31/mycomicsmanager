using Application.Books.List;
using Application.Interfaces;
using AwesomeAssertions;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using NSubstitute;
using Web.Models;
using Web.Services;
using Xunit;

namespace Web.Tests.Models;

public sealed class LibraryBookRowsTests
{
    private const int PageSize = 4;

    private static readonly Guid s_libraryId = Guid.CreateVersion7();

    private readonly IBooksService _booksService = Substitute.For<IBooksService>();
    private readonly List<BookSummaryDto> _books = [.. Enumerable.Range(0, 10).Select(i => new BookSummaryDto { Id = Guid.CreateVersion7(), Title = $"Tome {i}", Serie = "Blacksad", VolumeNumber = i })];

    public LibraryBookRowsTests()
    {
        _booksService.GetPagedByLibrary(s_libraryId, Arg.Any<int>(), PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>())
            .Returns(call => Page(call.ArgAt<int>(1)));
    }

    private Result<IPagedList<BookSummaryDto>> Page(int page)
    {
        var paged = Substitute.For<IPagedList<BookSummaryDto>>();
        paged.Items.Returns(_books.Skip((page - 1) * PageSize).Take(PageSize).ToList());
        paged.TotalCount.Returns(_books.Count);
        return Result.Success(paged);
    }

    private LibraryBookRows Create(BookSortOrder sortOrder = BookSortOrder.IdAsc, string search = "") =>
        new(_booksService, s_libraryId, sortOrder, search, PageSize);

    private static IEnumerable<string> Titles(VirtualRow<BookListItemViewModel> row) => row.Items.Select(b => b.Title);

    [Fact]
    public async Task GetRowsAsync_Should_CutTheBooksInRowsOfAsManyBooksAsColumns_FromThePagesThatHoldThem()
    {
        var rows = Create();

        var result = await rows.GetRowsAsync(1, 2, 3, TestContext.Current.CancellationToken);

        result.Value!.RowCount.Should().Be(4);
        result.Value.Rows.Select(r => r.FirstItem).Should().Equal(3, 6);
        Titles(result.Value.Rows[0]).Should().Equal("Tome 3", "Tome 4", "Tome 5");
        Titles(result.Value.Rows[1]).Should().Equal("Tome 6", "Tome 7", "Tome 8");
        await _booksService.Received(1).GetPagedByLibrary(s_libraryId, 1, PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>());
        await _booksService.Received(1).GetPagedByLibrary(s_libraryId, 2, PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>());
        await _booksService.Received(1).GetPagedByLibrary(s_libraryId, 3, PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRowsAsync_Should_StopAtTheLastBook_AndLoadEachPageOnce_WhenTheColumnsChange()
    {
        var rows = Create();
        await rows.GetRowsAsync(0, 10, 3, TestContext.Current.CancellationToken);

        var result = await rows.GetRowsAsync(0, 10, 4, TestContext.Current.CancellationToken);

        result.Value!.RowCount.Should().Be(3);
        result.Value.Rows.Should().HaveCount(3);
        Titles(result.Value.Rows[2]).Should().Equal("Tome 8", "Tome 9");
        await _booksService.Received(3).GetPagedByLibrary(s_libraryId, Arg.Any<int>(), PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>());
        rows.LoadedBooks.Select(b => b.Title).Should().Equal(_books.Select(b => b.Title));
    }

    [Fact]
    public async Task GetRowsAsync_Should_SkipTheBooksThatLeftTheLibrary_WhenAPageHoldsFewerBooksThanCounted()
    {
        var shortPage = Substitute.For<IPagedList<BookSummaryDto>>();
        shortPage.Items.Returns(_books.Take(2).ToList());
        shortPage.TotalCount.Returns(3);
        _booksService.GetPagedByLibrary(s_libraryId, 1, PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>())
            .Returns(Result.Success(shortPage));

        var result = await Create().GetRowsAsync(0, 1, 3, TestContext.Current.CancellationToken);

        Titles(result.Value!.Rows[0]).Should().Equal("Tome 0", "Tome 1");
    }

    [Fact]
    public async Task GetRowsAsync_Should_ReturnTheError_WhenTheFirstPageCannotBeLoaded()
    {
        _booksService.GetPagedByLibrary(s_libraryId, 1, PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IPagedList<BookSummaryDto>>(BooksError.BadRequest));

        var result = await Create().GetRowsAsync(0, 2, 3, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public async Task GetRowsAsync_Should_ReturnTheError_WhenAFurtherPageCannotBeLoaded()
    {
        _booksService.GetPagedByLibrary(s_libraryId, 2, PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IPagedList<BookSummaryDto>>(BooksError.BadRequest));
        var rows = Create();

        var result = await rows.GetRowsAsync(0, 2, 3, TestContext.Current.CancellationToken);

        result.Error.Should().Be(BooksError.BadRequest);
        rows.Find(_books[1].Id)!.Title.Should().Be("Tome 1");
        rows.Find(Guid.CreateVersion7()).Should().BeNull();
    }

    [Theory]
    [InlineData(-1, 1, 3)]
    [InlineData(0, -1, 3)]
    [InlineData(0, 1, 0)]
    public async Task GetRowsAsync_Should_Throw_WhenTheRangeIsNotValid(int startRow, int rowCount, int columns)
    {
        var act = () => Create().GetRowsAsync(startRow, rowCount, columns, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task LoadFirstPageAsync_Should_CountTheBooks_AndKeepTheFirstPageForTheRows()
    {
        var rows = Create();

        var count = await rows.LoadFirstPageAsync(TestContext.Current.CancellationToken);
        var result = await rows.GetRowsAsync(0, 1, 3, TestContext.Current.CancellationToken);

        count.Value.Should().Be(10);
        Titles(result.Value!.Rows[0]).Should().Equal("Tome 0", "Tome 1", "Tome 2");
        await _booksService.Received(1).GetPagedByLibrary(s_libraryId, 1, PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadFirstPageAsync_Should_ReturnTheError_WhenTheFirstPageCannotBeLoaded()
    {
        _booksService.GetPagedByLibrary(s_libraryId, 1, PageSize, BookSortOrder.IdAsc, "", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IPagedList<BookSummaryDto>>(BooksError.BadRequest));

        var count = await Create().LoadFirstPageAsync(TestContext.Current.CancellationToken);

        count.Error.Should().Be(BooksError.BadRequest);
    }

    [Fact]
    public void ShowsOtherItemsThan_Should_BeTrue_ForAnotherLibrary() =>
        Create().ShowsOtherItemsThan(new LibraryBookRows(_booksService, Guid.CreateVersion7(), BookSortOrder.IdAsc, "", PageSize)).Should().BeTrue();

    [Theory]
    [InlineData(BookSortOrder.IdAsc, "", false)]
    [InlineData(BookSortOrder.IdDesc, "", true)]
    [InlineData(BookSortOrder.IdAsc, "Blacksad", true)]
    public void ShowsOtherItemsThan_Should_TellWhetherTheSortOrTheSearchChanged(BookSortOrder sortOrder, string search, bool expected)
    {
        Create(sortOrder, search).ShowsOtherItemsThan(Create()).Should().Be(expected);
        Create().ShowsOtherItemsThan(null).Should().BeTrue();
    }
}
