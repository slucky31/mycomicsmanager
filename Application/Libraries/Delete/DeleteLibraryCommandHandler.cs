using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.Delete;

public sealed class DeleteLibraryCommandHandler(IRepository<Library, Guid> librayRepository, IUnitOfWork unitOfWork, ILibraryLocalStorage libraryLocalStorage) : ICommandHandler<DeleteLibraryCommand>
{
    public async Task<Result> Handle(DeleteLibraryCommand request, CancellationToken cancellationToken)
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

        var result = libraryLocalStorage.Delete(library.RelativePath);
        if (result.IsFailure)
        {
            return LibrariesError.FolderNotDeleted;
        }

        librayRepository.Remove(library);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
