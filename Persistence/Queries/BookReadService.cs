using Application.Books.List;
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
            query = query.Where(b =>
                EF.Functions.ILike(b.Title, $"%{searchTerm}%") ||
                (b.Serie != null && EF.Functions.ILike(b.Serie, $"%{searchTerm}%")));
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
                .Select(rd => rd.Rating)
                .FirstOrDefault()
        });
#pragma warning restore CA1826

        return await new PagedList<BookSummaryDto>(projected).ExecuteQueryAsync(page, pageSize, cancellationToken);
    }
}
