using System.Linq.Expressions;
using Application.Helpers;
using Application.Interfaces;
using Domain.Libraries;
using MediatR;

// Source : https://www.youtube.com/watch?v=X8zRvXbirMU

namespace Application.Libraries.List;
internal sealed class GetLibrariesQueryHandler : IRequestHandler<GetLibrariesQuery, PagedList<Library>>
{
    private readonly IApplicationDbContext _context;

    public GetLibrariesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<Library>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Library> librariesQuery = _context.Libraries;

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

        var librariesDtoPagedList = await PagedList<Library>.CreateAsync(librariesQuery, request.Page, request.PageSize);

        // Normalement, on devrait retourner un type Librairie Response
        // Mais MongoDb Entity Framework ne supporte pas (encore) la fonction Select ...
        // Donc pour l'instant, on fait avec le DTO

        return librariesDtoPagedList;        
    }
}
