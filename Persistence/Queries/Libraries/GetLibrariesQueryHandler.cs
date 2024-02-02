using System.Linq.Expressions;
using Application.Interfaces;
using Application.Libraries.List;
using Domain.Libraries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.Queries.Helpers;

// Source : https://www.youtube.com/watch?v=X8zRvXbirMU

namespace Persistence.Queries.Libraries;
internal sealed class GetLibrariesQueryHandler : IRequestHandler<GetLibrariesQuery, IPagedList<Library>>
{
    private readonly ApplicationDbContext _context;

    public GetLibrariesQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IPagedList<Library>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Library> librariesQuery = _context.Libraries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            librariesQuery = librariesQuery.Where(l => l.Name.Contains(request.SearchTerm));
        }

        Expression<Func<Library, object>> keySelector = request.SortColumn?.ToUpperInvariant() switch
        {
            "name" => Library => Library.Name,
            "id" => Library => Library.Id,
            _ => Library => Library.Id
        };

        if (request.SortOrder?.ToUpperInvariant() == "desc")
        {
            librariesQuery = librariesQuery.OrderByDescending(keySelector);
        }
        else
        {
            librariesQuery = librariesQuery.OrderBy(keySelector);
        }

        var librariesPagedList = new PagedList<Library>(librariesQuery);
        await librariesPagedList.ExecuteQueryAsync(request.Page, request.PageSize);
        
        return librariesPagedList;        
    }
}
