using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using Persistence.Queries.Helpers;

namespace Persistence.Queries;

public class BookReadService(ApplicationDbContext context) : IBookReadService
{
    public async Task<IPagedList<Book>> GetPagedByLibraryAsync(
        Guid libraryId,
        Guid userId,
        int page,
        int pageSize,
        BookSortOrder sortOrder,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<Book>()
            .Include(b => b.ReadingDates)
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
                .ThenBy(b => b.VolumeNumber),
            _ => query.OrderByDescending(b => b.Id)
        };

        return await new PagedList<Book>(query).ExecuteQueryAsync(page, pageSize, cancellationToken);
    }
}
