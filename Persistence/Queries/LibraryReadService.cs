using System.Linq.Expressions;
using Application.Interfaces;
using Application.Libraries;
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
