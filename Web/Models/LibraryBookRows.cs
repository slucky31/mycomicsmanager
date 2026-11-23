using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Web.Services;

namespace Web.Models;

// The books of a library for one sort and one search, loaded page by page as the rows are asked for (#1344):
// a page is loaded once, whatever the number of columns, so a rotation of the screen loads nothing again.
public sealed class LibraryBookRows(IBooksService booksService, Guid libraryId, BookSortOrder sortOrder, string search, int pageSize = LibraryBookRows.DefaultPageSize)
    : IVirtualRowSource<BookListItemViewModel>
{
    public const int DefaultPageSize = 48;

    private readonly Dictionary<int, IReadOnlyList<BookListItemViewModel>> _pages = [];
    private int? _bookCount;

    public Guid LibraryId { get; } = libraryId;
    public BookSortOrder SortOrder { get; } = sortOrder;
    public string Search { get; } = search;

    // The books loaded so far, in their order.
    public IEnumerable<BookListItemViewModel> LoadedBooks => _pages.OrderBy(p => p.Key).SelectMany(p => p.Value);

    public BookListItemViewModel? Find(Guid bookId) => LoadedBooks.FirstOrDefault(b => b.Id == bookId);

    // Whether the rows are those of another library, sort or search, which start at the top again.
    public bool ShowsOtherItemsThan(IVirtualRowSource<BookListItemViewModel>? other) =>
        other is not LibraryBookRows rows || rows.LibraryId != LibraryId || rows.SortOrder != SortOrder || !string.Equals(rows.Search, Search, StringComparison.Ordinal);

    // The first page, loaded before the rows are shown: it tells how many books there are (none: the library is empty).
    public async Task<Result<int>> LoadFirstPageAsync(CancellationToken cancellationToken)
    {
        var loaded = await LoadPageAsync(0, cancellationToken);
        return loaded.IsSuccess ? _bookCount!.Value : loaded.Error!;
    }

    public async Task<Result<VirtualRowsPage<BookListItemViewModel>>> GetRowsAsync(int startRow, int rowCount, int columns, CancellationToken cancellationToken)
    {
        VirtualRows.CheckRequest(startRow, rowCount, columns);

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

        return new VirtualRowsPage<BookListItemViewModel>([.. RowsOf(first, end, columns)], VirtualRows.CountOf(_bookCount.Value, columns));
    }

    private IEnumerable<VirtualRow<BookListItemViewModel>> RowsOf(int first, int end, int columns)
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

            yield return new VirtualRow<BookListItemViewModel>(start, books);
        }
    }

    private async Task<Result> LoadPageAsync(int page, CancellationToken cancellationToken)
    {
        var result = await booksService.GetPagedByLibrary(LibraryId, page + 1, pageSize, SortOrder, Search, cancellationToken);
        if (result.IsFailure || result.Value?.Items is null)
        {
            return result.Error ?? BooksError.NotFound;
        }

        _pages[page] = [.. result.Value.Items.Select(BookListItemViewModel.From)];
        _bookCount = result.Value.TotalCount;
        return Result.Success();
    }
}
