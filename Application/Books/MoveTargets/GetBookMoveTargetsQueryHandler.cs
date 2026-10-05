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
    private const int MaxLibraries = 500;

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

        var libraries = await libraryReadService.GetLibrariesAsync(
            null, LibrariesColumn.Name, SortOrder.Ascending, 1, MaxLibraries, request.UserId, cancellationToken);
        var candidates = (libraries.Items ?? [])
            .Where(l => l.Id != source.Id && l.BookType == source.BookType)
            .ToList();
        if (candidates.Count == 0)
        {
            return new BookMoveTargets([], null);
        }

        var sameSerieCounts = await CountSameSerieByLibraryAsync(book.Serie, request.UserId, cancellationToken);
        var targets = candidates
            .Select(l => new BookMoveTarget(l.Id, l.Name, l.Color, l.Icon, sameSerieCounts.GetValueOrDefault(l.Id)))
            .ToList();
        var suggested = targets
            .Where(t => t.SameSerieCount > 0)
            .OrderByDescending(t => t.SameSerieCount)
            .ThenBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();

        return new BookMoveTargets(targets, suggested?.Id);
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
