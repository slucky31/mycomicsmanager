using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Librairies.Get;

internal sealed class GetLibraryQueryHandler : IRequestHandler<GetLibraryQuery, Result<Library>>
{
    private readonly IRepository<Library, string> _librayRepository;

    public GetLibraryQueryHandler(IRepository<Library, string> librayRepository)
    {
        _librayRepository = librayRepository;
    }

    public async Task<Result<Library>> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
    {
        var library = await _librayRepository.GetByIdAsync(request.LibraryId);
        if (library is null)
        {
            return LibrariesErrors.NotFound;
        }

        return library;
    }
}
