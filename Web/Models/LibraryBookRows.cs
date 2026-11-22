using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Web.Services;

namespace Web.Models;

// A row of the virtualized library (#1344): as many books as the grid has columns, from the book at FirstBook.
public sealed record BookRow(int FirstBook, IReadOnlyList<BookListItemViewModel> Books);

// The same row when it starts at the same place with the same book (Virtualize keeps it on screen when rows arrive).
public sealed class BookRowComparer : IEqualityComparer<BookRow>
{
    public static readonly BookRowComparer Instance = new();

    public bool Equals(BookRow? x, BookRow? y) =>
        ReferenceEquals(x, y) || (x is not null && y is not null && x.FirstBook == y.FirstBook && FirstId(x) == FirstId(y));

    public int GetHashCode(BookRow obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return HashCode.Combine(obj.FirstBook, FirstId(obj));
    }

    private static Guid? FirstId(BookRow row) => row.Books.Count > 0 ? row.Books[0].Id : null;
}

// The rows asked for, and how many rows the library has.
public sealed record BookRows(IReadOnlyList<BookRow> Rows, int RowCount);

// The books of a library for one sort and one search, loaded page by page as the rows are asked for (#1344):
// a page is loaded once, whatever the number of columns, so a rotation of the screen loads nothing again.
public sealed class LibraryBookRows(IBooksService booksService, Guid libraryId, BookSortOrder sortOrder, string search, int pageSize = LibraryBookRows.DefaultPageSize)
{
    public const int DefaultPageSize = 48;

    private readonly Dictionary<int, IReadOnlyList<BookListItemViewModel>> _pages = [];
    private int? _bookCount;

    public BookSortOrder SortOrder { get; } = sortOrder;
    public string Search { get; } = search;

    // The books loaded so far, in their order.
    public IEnumerable<BookListItemViewModel> LoadedBooks => _pages.OrderBy(p => p.Key).SelectMany(p => p.Value);

    public BookListItemViewModel? Find(Guid bookId) => LoadedBooks.FirstOrDefault(b => b.Id == bookId);

    // Whether the rows are those of another sort or search, which start at the top again.
    public bool ShowsOtherBooksThan(LibraryBookRows? other) =>
        other is null || other.SortOrder != SortOrder || !string.Equals(other.Search, Search, StringComparison.Ordinal);

    public async Task<Result<BookRows>> GetRowsAsync(int startRow, int rowCount, int columns, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startRow);
        ArgumentOutOfRangeException.ThrowIfNegative(rowCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);

        var first = startRow * columns;

        // The first page tells how many books there are.
        if (_bookCount is null)
        {
            var loaded = await LoadPageAsync(first / pageSize, cancellationToken);
            if (loaded.IsFailure)
            {
                return loaded.Error!;
            }
        }

        var end = Math.Min(first + (rowCount * columns), _bookCount!.Value);
        for (var page = first / pageSize; page * pageSize < end; page++)
        {
            if (!_pages.ContainsKey(page))
            {
                var loaded = await LoadPageAsync(page, cancellationToken);
                if (loaded.IsFailure)
                {
                    return loaded.Error!;
                }
            }
        }

        var rowTotal = (_bookCount.Value + columns - 1) / columns;
        return new BookRows([.. RowsOf(first, end, columns)], rowTotal);
    }

    private IEnumerable<BookRow> RowsOf(int first, int end, int columns)
    {
        for (var start = first; start < end; start += columns)
        {
            var books = new List<BookListItemViewModel>(columns);
            for (var index = start; index < Math.Min(start + columns, end); index++)
            {
                // A page can hold fewer books than counted when books left the library meanwhile.
                if (_pages.TryGetValue(index / pageSize, out var page) && index % pageSize < page.Count)
                {
                    books.Add(page[index % pageSize]);
                }
            }

            yield return new BookRow(start, books);
        }
    }

    private async Task<Result> LoadPageAsync(int page, CancellationToken cancellationToken)
    {
        var result = await booksService.GetPagedByLibrary(libraryId, page + 1, pageSize, SortOrder, Search, cancellationToken);
        if (result.IsFailure || result.Value?.Items is null)
        {
            return result.Error ?? BooksError.NotFound;
        }

        _pages[page] = [.. result.Value.Items.Select(BookListItemViewModel.From)];
        _bookCount = result.Value.TotalCount;
        return Result.Success();
    }
}
