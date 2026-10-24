using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries;
using Application.Users;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.FeedImports;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Users;

namespace Application.Books.MoveTargets;

public sealed class GetBookMoveTargetsQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository,
    ILibraryReadService libraryReadService,
    IBookReadService bookReadService,
    IRepository<User, Guid> userRepository) : IQueryHandler<GetBookMoveTargetsQuery, BookMoveTargets>
{
    // A selection of a library holds at most the books loaded on its page.
    public const int MaxBooks = 500;

    public async Task<Result<BookMoveTargets>> Handle(GetBookMoveTargetsQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        if (request.BookIds is null || request.BookIds.Count == 0 || request.BookIds.Count > MaxBooks
            || request.BookIds.Contains(Guid.Empty) || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var books = await LoadBooksAsync(request.BookIds.Distinct().ToList());
        var source = books is null ? null : await libraryRepository.GetByIdAsync(books[0].LibraryId);
        if (books is null || source is null || source.UserId != request.UserId)
        {
            return BooksError.NotFound;
        }

        // Ordered by name, with their book counts; a hidden library is a target only when the user shows them.
        var includeHidden = await userRepository.ShowsHiddenLibrariesAsync(request.UserId);
        var libraries = await libraryReadService.GetSummariesAsync(request.UserId, includeHidden, cancellationToken);
        var current = libraries.FirstOrDefault(l => l.Id == source.Id) is { } summary
            ? new BookMoveTarget(summary.Id, summary.Name, summary.Color, summary.Icon, 0, summary.BookCount)
            : new BookMoveTarget(source.Id, source.Name, source.Color, source.Icon, 0);
        var candidates = libraries
            .Where(l => l.Id != source.Id && l.BookType == source.BookType)
            .ToList();
        if (candidates.Count == 0)
        {
            return new BookMoveTargets([], null) { Current = current, BookType = source.BookType, BookCount = books.Count };
        }

        var sameSerieCounts = await CountSameSeriesByLibraryAsync(books, request.UserId, cancellationToken);
        var targets = candidates
            .Select(l => new BookMoveTarget(l.Id, l.Name, l.Color, l.Icon, sameSerieCounts.GetValueOrDefault(l.Id), l.BookCount))
            .ToList();
        var suggested = targets
            .Where(t => t.SameSerieCount > 0)
            .OrderByDescending(t => t.SameSerieCount)
            .ThenBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();

        return new BookMoveTargets(targets, suggested?.Id) { Current = current, BookType = source.BookType, BookCount = books.Count };
    }

    // Null when a book is missing or the books are not in the same library.
    private async Task<List<Book>?> LoadBooksAsync(List<Guid> bookIds)
    {
        List<Book> books = [];
        foreach (var bookId in bookIds)
        {
            var book = await bookRepository.GetByIdAsync(bookId);
            if (book is null || (books.Count > 0 && book.LibraryId != books[0].LibraryId))
            {
                return null;
            }

            books.Add(book);
        }

        return books;
    }

    // Series are compared normalized (case, accents, punctuation and leading article ignored).
    private async Task<Dictionary<Guid, int>> CountSameSeriesByLibraryAsync(List<Book> books, Guid userId, CancellationToken cancellationToken)
    {
        var series = books
            .Select(b => SerieMatcher.Normalize(b.Serie))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet(StringComparer.Ordinal);
        if (series.Count == 0)
        {
            return [];
        }

        var locations = await bookReadService.ListSerieLocationsAsync(userId, cancellationToken);
        return locations
            .Where(l => series.Contains(SerieMatcher.Normalize(l.Serie)))
            .GroupBy(l => l.LibraryId)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
