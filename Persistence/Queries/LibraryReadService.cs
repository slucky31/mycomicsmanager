using System.Linq.Expressions;
using Application.Interfaces;
using Application.Libraries;
using Application.Libraries.Summaries;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Persistence.Queries.Helpers;

namespace Persistence.Queries;

public class LibraryReadService(ApplicationDbContext context) : ILibraryReadService
{
    public async Task<IPagedList<Library>> GetLibrariesAsync(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize, Guid userId, CancellationToken cancellationToken = default)
    {
        var librariesQuery = context.Libraries.AsNoTracking().Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            librariesQuery = librariesQuery.Where(l => l.Name.Contains(searchTerm));
        }

        Expression<Func<Library, object>> keySelector = sortColumn switch
        {
            LibrariesColumn.Id => library => library.Id,
            LibrariesColumn.Name => library => library.Name,
            _ => library => library.Id
        };

        librariesQuery = sortOrder switch
        {
            SortOrder.Descending => librariesQuery.OrderByDescending(keySelector),
            SortOrder.Ascending => librariesQuery.OrderBy(keySelector),
            _ => librariesQuery.OrderBy(keySelector),
        };
        var librariesPagedList = new PagedList<Library>(librariesQuery);
        return await librariesPagedList.ExecuteQueryAsync(page, pageSize, cancellationToken);
    }

    public async Task<Library?> GetByNameAsync(string name, Guid userId, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();

#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
        return await context.Libraries.AsNoTracking()
            .Where(l => l.UserId == userId && l.Name.ToUpper() == normalizedName)
            .OrderBy(l => l.Id)
            .FirstOrDefaultAsync(cancellationToken);
#pragma warning restore CA1311 // Specify a culture or use an invariant version
#pragma warning restore CA1304 // Specify CultureInfo
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
    }

    public async Task<IReadOnlyList<LibrarySummary>> GetSummariesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Libraries.AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Name)
            .ThenBy(l => l.Id)
            .Select(l => new LibrarySummary(
                l.Id,
                l.Name,
                l.Color,
                l.Icon,
                l.BookType,
                context.Books.Count(b => b.LibraryId == l.Id))
            {
                Covers = context.Books
                    .Where(b => b.LibraryId == l.Id && b.ImageLink != "")
                    .OrderByDescending(b => b.Id)
                    .Select(b => b.ImageLink)
                    .Take(LibrarySummary.MaxCovers)
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Library>> ListByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();

#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
        return await context.Libraries.AsNoTracking()
            .Where(l => l.Name.ToUpper() == normalizedName)
            .OrderBy(l => l.Id)
            .ToListAsync(cancellationToken);
#pragma warning restore CA1311 // Specify a culture or use an invariant version
#pragma warning restore CA1304 // Specify CultureInfo
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid userId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();

#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
#pragma warning disable CA1304 // Specify CultureInfo
#pragma warning disable CA1311 // Specify a culture or use an invariant version
        var query = context.Libraries.AsNoTracking()
            .Where(l => l.UserId == userId && l.Name.ToUpper() == normalizedName); // ✅ Sinon cela plante dans EFCore
#pragma warning restore CA1311 // Specify a culture or use an invariant version
#pragma warning restore CA1304 // Specify CultureInfo
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

        if (excludeId.HasValue)
        {
            query = query.Where(l => l.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
