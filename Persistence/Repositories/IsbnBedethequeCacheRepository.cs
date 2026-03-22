using Application.Interfaces;
using Domain.Books;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Persistence.Repositories;

public class IsbnBedethequeCacheRepository(ApplicationDbContext dbContext) : IIsbnBedethequeCacheRepository
{
    public async Task<string?> GetUrlByIsbnAsync(string isbn, CancellationToken ct = default)
    {
        return await dbContext.IsbnBedethequeUrls
            .Where(x => x.ISBN == isbn)
            .Select(x => x.Url)
            .FirstOrDefaultAsync(ct);
    }

    public async Task SaveAsync(string isbn, string url, CancellationToken ct = default)
    {
        var entity = IsbnBedethequeUrl.Create(isbn, url);
        dbContext.IsbnBedethequeUrls.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Another concurrent request already cached this ISBN → ignore
            dbContext.Entry(entity).State = EntityState.Detached;
        }
    }
}
