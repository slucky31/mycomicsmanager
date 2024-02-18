using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Libraries.ReadService;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Persistence.Queries.Helpers;

namespace Persistence.Queries;
public class LibraryReadService : ILibraryReadService
{
    private readonly ApplicationDbContext _context;

    public LibraryReadService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IPagedList<Library>> GetLibrariesAsync(string? searchTerm, LibrariesColumn? sortColumn, SortOrder? sortOrder, int page, int pageSize)
    {
        IQueryable<Library> librariesQuery = _context.Libraries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            librariesQuery = librariesQuery.Where(l => l.Name.Contains(searchTerm));
        }

        Expression<Func<Library, object>> keySelector = sortColumn switch
        {
            LibrariesColumn.Id => Library => Library.Id,
            LibrariesColumn.Name => Library => Library.Name,
            _ => Library => Library.Id
        };

        if (sortOrder == SortOrder.Descending)
        {
            librariesQuery = librariesQuery.OrderByDescending(keySelector);
        }
        else
        {
            librariesQuery = librariesQuery.OrderBy(keySelector);
        }

        var librariesPagedList = new PagedList<Library>(librariesQuery);
        return await librariesPagedList.ExecuteQueryAsync(page, pageSize);        
    }
}
