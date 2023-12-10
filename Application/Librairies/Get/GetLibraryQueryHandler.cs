using Domain.Libraries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Librairies.Get;

internal sealed class GetLibraryQueryHandler : IRequestHandler<GetLibraryQuery, Library>
{
    private readonly IApplicationDbContext _context;

    public GetLibraryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Library> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
    {
        var library = await _context.Libraries             
             .FirstOrDefaultAsync(l => l.Id == request.LibraryId, cancellationToken);

        if (library is null)
        {
            // TODO
            throw new ArgumentNullException(nameof(request));
        }

        return library;
    }
}
