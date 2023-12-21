using System.Linq.Expressions;
using Application.Helpers;
using Application.Interfaces;
using Domain.Dto;
using MediatR;

// Source : https://www.youtube.com/watch?v=X8zRvXbirMU

namespace Application.Libraries.Get;
internal sealed class GetLibrariesQueryHandler : IRequestHandler<GetLibrariesQuery, PagedList<LibraryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLibrariesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<LibraryDto>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<LibraryDto> librariesDtoQuery = _context.Libraries;

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            librariesDtoQuery = librariesDtoQuery.Where(l => l.Name.Contains(request.SearchTerm));
        }

        Expression<Func<LibraryDto, object>> keySelector = request.SortColumn?.ToLowerInvariant() switch
        {
            "name" => Library => Library.Name,            
            _ => Library => Library.Id
        };

        if (request.SortOrder?.ToLower() == "desc")
        {
            librariesDtoQuery = librariesDtoQuery.OrderByDescending(keySelector);
        }
        else
        {
            librariesDtoQuery = librariesDtoQuery.OrderBy(keySelector);
        }

        var librariesDtoPagedList = await PagedList<LibraryDto>.CreateAsync(librariesDtoQuery, request.Page, request.PageSize);

        // Normalement, on devrait retourner un type Librairie Response
        // Mais MongoDb Entity Framework ne supporte pas (encore) la fonction Select ...
        // Donc pour l'instant, on fait avec le DTO

        return librariesDtoPagedList;        
    }
}
