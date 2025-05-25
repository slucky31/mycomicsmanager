using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;
using MongoDB.Bson;

namespace Application.Libraries.Delete;

public sealed class DeleteLibraryCommandHandler(IRepository<Library, ObjectId> librayRepository, IUnitOfWork unitOfWork, ILibraryLocalStorage libraryLocalStorage) : ICommandHandler<DeleteLibraryCommand>
{
    public async Task<Result> Handle(DeleteLibraryCommand request, CancellationToken cancellationToken)
    {
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
