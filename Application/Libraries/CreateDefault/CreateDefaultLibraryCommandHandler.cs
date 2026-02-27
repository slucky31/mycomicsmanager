using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.CreateDefault;

public sealed class CreateDefaultLibraryCommandHandler(IRepository<Library, Guid> libraryRepository, IUnitOfWork unitOfWork, ILibraryReadService libraryReadService) : ICommandHandler<CreateDefaultLibraryCommand, Library>
{
    public async Task<Result<Library>> Handle(CreateDefaultLibraryCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: if a default library already exists for this user, return it
        var existingDefault = await libraryReadService.GetDefaultLibraryAsync(request.UserId, cancellationToken);
        if (existingDefault is not null)
        {
            return existingDefault;
        }

        var library = Library.CreateDefault(request.UserId);

        libraryRepository.Add(library);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return library;
    }
}
