using Application.Interfaces;
using Domain.Books;
using Microsoft.EntityFrameworkCore;

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
        await dbContext.SaveChangesAsync(ct);
    }
}
