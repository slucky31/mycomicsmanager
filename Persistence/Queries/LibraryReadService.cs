using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Libraries;
using Domain.Libraries;
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
    
    public async Task<IPagedList<Library>> GetLibrariesAsync(string? SearchTerm, string? SortColumn, string? SortOrder, int Page, int PageSize)
    {
        IQueryable<Library> librariesQuery = _context.Libraries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            librariesQuery = librariesQuery.Where(l => l.Name.Contains(SearchTerm));
        }

        Expression<Func<Library, object>> keySelector = SortColumn?.ToUpperInvariant() switch
        {
            "ID" => Library => Library.Id,
            "NAME" => Library => Library.Name,
            "RELPATH" => Library => Library.RelativePath,
            _ => Library => Library.Id
        };

        if (SortOrder?.ToUpperInvariant() == "DESC")
        {
            librariesQuery = librariesQuery.OrderByDescending(keySelector);
        }
        else
        {
            librariesQuery = librariesQuery.OrderBy(keySelector);
        }

        var librariesPagedList = new PagedList<Library>(librariesQuery);
        return await librariesPagedList.ExecuteQueryAsync(Page, PageSize);        
    }
}
