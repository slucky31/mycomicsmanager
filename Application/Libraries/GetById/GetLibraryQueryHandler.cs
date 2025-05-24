using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
using MongoDB.Bson;

namespace Application.Libraries.GetById;

public sealed class GetLibraryQueryHandler(IRepository<Library, ObjectId> librayRepository) : IQueryHandler<GetLibraryQuery, Library>
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
