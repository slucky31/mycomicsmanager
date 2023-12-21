using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using Application.Interfaces;
using Domain.Dto;

namespace Application.Librairies.Get;

internal sealed class GetLibraryQueryHandler : IRequestHandler<GetLibraryQuery, Result<Library>>
{
    private readonly IRepository<LibraryDto, LibraryId> _librayRepository;

    public GetLibraryQueryHandler(IRepository<LibraryDto, LibraryId> librayRepository)
    {
        _librayRepository = librayRepository;
    }

    public async Task<Result<Library>> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
    {
        var libraryDto = await _librayRepository.GetByIdAsync(request.Id);
        if (libraryDto is null)
        {
            return LibrariesErrors.NotFound;
        }

        return libraryDto.ToLibrary();
    }
}
