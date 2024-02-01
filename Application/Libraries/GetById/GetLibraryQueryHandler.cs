using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using Application.Interfaces;
using MongoDB.Bson;

namespace Application.Libraries.GetById;

internal sealed class GetLibraryQueryHandler : IRequestHandler<GetLibraryQuery, Result<Library>>
{
    private readonly IRepository<Library, ObjectId> _librayRepository;

    public GetLibraryQueryHandler(IRepository<Library, ObjectId> librayRepository)
    {
        _librayRepository = librayRepository;
    }

    public async Task<Result<Library>> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
    {
        var library = await _librayRepository.GetByIdAsync(request.Id);
        if (library is null)
        {
            return LibrariesErrors.NotFound;
        }

        return library;
    }
}
