using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.GetById;

public sealed class GetLibraryQueryHandler(IRepository<Library, Guid> librayRepository) : IQueryHandler<GetLibraryQuery, Library>
{
    public async Task<Result<Library>> Handle(GetLibraryQuery request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return LibrariesError.ValidationError;
        }

        var library = await librayRepository.GetByIdAsync(request.Id);
        if (library is null)
        {
            return LibrariesError.NotFound;
        }

        return library;
    }
}
