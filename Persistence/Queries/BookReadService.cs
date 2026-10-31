using Application.Books;
using Application.Books.List;
using Application.Books.Reading;
using Application.Books.Search;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using Persistence.Queries.Helpers;

namespace Persistence.Queries;

public class BookReadService(ApplicationDbContext context) : IBookReadService
{
    public async Task<IPagedList<BookSummaryDto>> GetPagedByLibraryAsync(
        Guid libraryId,
        Guid userId,
        int page,
        int pageSize,
        BookSortOrder sortOrder,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<Book>()
            .AsNoTracking()
            .Where(b => b.LibraryId == libraryId && b.Library!.UserId == userId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{LikePatternHelper.EscapeLikeSpecialChars(searchTerm)}%";
            query = query.Where(b =>
                EF.Functions.ILike(b.Title, pattern, @"\") ||
                (b.Serie != null && EF.Functions.ILike(b.Serie, pattern, @"\")));
        }

        query = sortOrder switch
        {
            BookSortOrder.IdAsc => query.OrderBy(b => b.Id),
            BookSortOrder.SerieAndVolumeAsc => query
                .OrderBy(b => b.Serie)
                .ThenBy(b => b.VolumeNumber)
                .ThenBy(b => b.Id),
            _ => query.OrderByDescending(b => b.Id)
        };

#pragma warning disable CA1826 // EF Core expression tree — cannot use indexer; LINQ methods are required for SQL translation
        var projected = query.Select(b => new BookSummaryDto
        {
            Id = b.Id,
            Serie = b.Serie,
            Title = b.Title,
            ISBN = b.ISBN,
            VolumeNumber = b.VolumeNumber,
            ImageLink = b.ImageLink,
            Authors = b.Authors,
            Publishers = b.Publishers,
            ReadCount = b.ReadingDates.Count(),
            LastRead = b.ReadingDates
                .OrderByDescending(rd => rd.Date)
                .Select(rd => (DateTime?)rd.Date)
                .FirstOrDefault(),
            LastRating = b.ReadingDates
                .OrderByDescending(rd => rd.Date)
                .Select(rd => (int?)rd.Rating)
                .FirstOrDefault(),
            // Null for a physical book (it always has an ISBN).
            IsbnCandidateCount = (int?)((DigitalBook)b).IsbnCandidates.Count,
            IsbnScannedAt = ((DigitalBook)b).IsbnScannedAt
        });
#pragma warning restore CA1826

        return await new PagedList<BookSummaryDto>(projected).ExecuteQueryAsync(page, pageSize, cancellationToken);
    }

    // At most this many series are found: a term matching more is too short to be useful.
    private const int SearchSerieLimit = 20;

    public async Task<BookSearchResult> SearchAsync(Guid userId, string searchTerm, int bookLimit, bool includeHidden, CancellationToken cancellationToken = default)
    {
        var pattern = $"%{LikePatternHelper.EscapeLikeSpecialChars(searchTerm)}%";
        var books = context.Set<Book>()
            .AsNoTracking()
            .Where(b => b.Library!.UserId == userId && (includeHidden || !b.Library.IsHidden));

        var series = await books
            .Where(b => b.Serie != null && b.Serie != "" && EF.Functions.ILike(b.Serie, pattern, @"\"))
            .GroupBy(b => new { b.LibraryId, b.Serie })
            .Select(g => new { g.Key.LibraryId, g.Key.Serie, Count = g.Count() })
            .OrderBy(s => s.Serie)
            .ThenBy(s => s.LibraryId)
            .Take(SearchSerieLimit)
            .ToListAsync(cancellationToken);

        var matching = books.Where(b =>
            EF.Functions.ILike(b.Title, pattern, @"\") ||
            (b.Serie != null && EF.Functions.ILike(b.Serie, pattern, @"\")));

        var bookCount = await matching.CountAsync(cancellationToken);
        var found = await matching
            .OrderBy(b => b.Serie)
            .ThenBy(b => b.VolumeNumber)
            .ThenBy(b => b.Title)
            .ThenBy(b => b.Id)
            .Take(bookLimit)
            .Select(b => new BookSearchHit(b.Id, b.LibraryId, b.Title, b.Serie, b.VolumeNumber, b.ImageLink))
            .ToListAsync(cancellationToken);

        return new BookSearchResult(
            [.. series.Select(s => new SerieSearchHit(s.LibraryId, s.Serie, s.Count))],
            found,
            bookCount);
    }

    public async Task<IReadOnlyList<BookInProgressDto>> ListInProgressAsync(Guid userId, bool includeHidden, CancellationToken cancellationToken = default)
        => await context.Set<DigitalBook>()
            .AsNoTracking()
            .Where(b => b.Library!.UserId == userId && (includeHidden || !b.Library.IsHidden))
            // Marking a book as read takes it back to the first page.
            .Where(b => b.LastReadPage > 0)
            // The books read before the reading time was recorded come last.
            .OrderByDescending(b => b.LastReadAt.HasValue)
            .ThenByDescending(b => b.LastReadAt)
            .ThenByDescending(b => b.Id)
            .Select(b => new BookInProgressDto(b.Id, b.LibraryId, b.Serie, b.Title, b.VolumeNumber, b.ImageLink, b.LastReadPage, b.NumberOfPages, b.LastReadAt))
            .ToListAsync(cancellationToken);

    public async Task<IPagedList<BookReadingDto>> GetRecentReadingsAsync(Guid userId, int page, int pageSize, bool includeHidden, CancellationToken cancellationToken = default)
    {
        var readings = context.Set<Book>()
            .AsNoTracking()
            .Where(b => b.Library!.UserId == userId && (includeHidden || !b.Library.IsHidden))
            .SelectMany(b => b.ReadingDates, (b, rd) => new { Book = b, Reading = rd })
            .OrderByDescending(x => x.Reading.Date)
            .ThenByDescending(x => x.Reading.Id)
            .Select(x => new BookReadingDto(
                x.Reading.Id,
                x.Book.Id,
                x.Book.LibraryId,
                x.Book.Serie,
                x.Book.Title,
                x.Book.VolumeNumber,
                x.Book.ImageLink,
                x.Reading.Date,
                x.Reading.Rating,
                x.Book.Library!.BookType == LibraryBookType.Digital));

        return await new PagedList<BookReadingDto>(readings).ExecuteQueryAsync(page, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<BookIdentityDto>> ListIdentitiesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.Set<Book>()
            .AsNoTracking()
            .Where(b => b.Library!.UserId == userId)
            .OrderBy(b => b.Id)
            .Select(b => new BookIdentityDto(b.Id, b.Serie, b.Title, b.VolumeNumber, b.ISBN, b.Library!.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BookSerieLocationDto>> ListSerieLocationsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.Set<Book>()
            .AsNoTracking()
            .Where(b => b.Library!.UserId == userId)
            .OrderBy(b => b.Id)
            .Select(b => new BookSerieLocationDto(b.LibraryId, b.Serie))
            .ToListAsync(cancellationToken);
}
