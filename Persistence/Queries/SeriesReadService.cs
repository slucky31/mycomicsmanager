using Application.Series;
using Domain.Books;
using Domain.Series;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Queries;

public class SeriesReadService(ApplicationDbContext context) : ISeriesReadService
{
    public async Task<IReadOnlyList<SerieBookRow>> ListBooksAsync(Guid userId, Guid? libraryId, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Book>().AsNoTracking().Where(b => b.Library!.UserId == userId);
        if (libraryId is { } id)
        {
            query = query.Where(b => b.LibraryId == id);
        }

        return await query
            .OrderBy(b => b.VolumeNumber)
            .ThenBy(b => b.Id)
            .Select(b => new SerieBookRow(b.Id, b.LibraryId, b.Library!.Name, b.Serie, b.Title, b.VolumeNumber, b.ImageLink))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SeriesInfo>> ListInfosAsync(Guid userId, Guid? libraryId, CancellationToken cancellationToken = default)
    {
        var query = context.SeriesInfos.AsNoTracking()
            .Where(s => context.Libraries.Any(l => l.Id == s.LibraryId && l.UserId == userId));
        if (libraryId is { } id)
        {
            query = query.Where(s => s.LibraryId == id);
        }

        return await query.OrderBy(s => s.Id).ToListAsync(cancellationToken);
    }
}
