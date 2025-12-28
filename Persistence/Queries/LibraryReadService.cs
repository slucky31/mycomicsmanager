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
    public async Task<IPagedList<Library>> GetLibrariesAsync(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var librariesQuery = context.Libraries.AsNoTracking();

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
}
