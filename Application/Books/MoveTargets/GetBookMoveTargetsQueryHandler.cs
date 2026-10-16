using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.FeedImports;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.MoveTargets;

public sealed class GetBookMoveTargetsQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository,
    ILibraryReadService libraryReadService,
    IBookReadService bookReadService) : IQueryHandler<GetBookMoveTargetsQuery, BookMoveTargets>
{
    public async Task<Result<BookMoveTargets>> Handle(GetBookMoveTargetsQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        if (request.BookId == Guid.Empty || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        var book = await bookRepository.GetByIdAsync(request.BookId);
        var source = book is null ? null : await libraryRepository.GetByIdAsync(book.LibraryId);
        if (book is null || source is null || source.UserId != request.UserId)
        {
            return BooksError.NotFound;
        }

        // Ordered by name, with their book counts.
        var libraries = await libraryReadService.GetSummariesAsync(request.UserId, cancellationToken);
        var current = libraries.FirstOrDefault(l => l.Id == source.Id) is { } summary
            ? new BookMoveTarget(summary.Id, summary.Name, summary.Color, summary.Icon, 0, summary.BookCount)
            : new BookMoveTarget(source.Id, source.Name, source.Color, source.Icon, 0);
        var candidates = libraries
            .Where(l => l.Id != source.Id && l.BookType == source.BookType)
            .ToList();
        if (candidates.Count == 0)
        {
            return new BookMoveTargets([], null) { Current = current, BookType = source.BookType };
        }

        var sameSerieCounts = await CountSameSerieByLibraryAsync(book.Serie, request.UserId, cancellationToken);
        var targets = candidates
            .Select(l => new BookMoveTarget(l.Id, l.Name, l.Color, l.Icon, sameSerieCounts.GetValueOrDefault(l.Id), l.BookCount))
            .ToList();
        var suggested = targets
            .Where(t => t.SameSerieCount > 0)
            .OrderByDescending(t => t.SameSerieCount)
            .ThenBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();

        return new BookMoveTargets(targets, suggested?.Id) { Current = current, BookType = source.BookType };
    }

    // Series are compared normalized (case, accents, punctuation and leading article ignored).
    private async Task<Dictionary<Guid, int>> CountSameSerieByLibraryAsync(string serie, Guid userId, CancellationToken cancellationToken)
    {
        var normalized = SerieMatcher.Normalize(serie);
        if (string.IsNullOrEmpty(normalized))
        {
            return [];
        }

        var locations = await bookReadService.ListSerieLocationsAsync(userId, cancellationToken);
        return locations
            .Where(l => SerieMatcher.Normalize(l.Serie) == normalized)
            .GroupBy(l => l.LibraryId)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
