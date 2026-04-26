using Application.Abstractions.Messaging;
using Application.ImportJobs;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.Delete;

public sealed class DeleteLibraryCommandHandler(IRepository<Library, Guid> librayRepository, IUnitOfWork unitOfWork, ILibraryLocalStorage libraryLocalStorage, IImportDirectoryStorage importDirectoryStorage) : ICommandHandler<DeleteLibraryCommand>
{
    public async Task<Result> Handle(DeleteLibraryCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return LibrariesError.ValidationError;
        }

        var library = await librayRepository.GetByIdAsync(request.Id);

        if (library is null || library.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        // Delete folders only for digital libraries
        if (library.BookType == LibraryBookType.Digital)
        {
            var storageResult = libraryLocalStorage.Delete(library.RelativePath);
            if (storageResult.IsFailure)
            {
                return LibrariesError.FolderNotDeleted;
            }

            importDirectoryStorage.Delete(library.ImportDirectoryName);
        }

        librayRepository.Remove(library);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
