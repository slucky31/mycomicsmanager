using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;

namespace Application.Libraries.GetById;

internal sealed class GetLibraryQueryHandler(IRepository<Library, ObjectId> librayRepository) : IRequestHandler<GetLibraryQuery, Result<Library>>
{
    public async Task<Result<Library>> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
    {
        var library = await librayRepository.GetByIdAsync(request.Id);
        if (library is null)
        {
            return LibrariesError.NotFound;
        }

        return library;
    }
}
