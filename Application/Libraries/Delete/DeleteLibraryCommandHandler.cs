using Application.Abstractions.Messaging;
using Application.Books;
using Application.ImportJobs;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.Delete;

public sealed class DeleteLibraryCommandHandler(
    IRepository<Library, Guid> librayRepository,
    IUnitOfWork unitOfWork,
    ILibraryLocalStorage libraryLocalStorage,
    IImportDirectoryStorage importDirectoryStorage,
    IBookRepository bookRepository,
    BookCoverStore coverStore) : ICommandHandler<DeleteLibraryCommand>
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

        // The feed imports deposit their files there.
        if (library.IsImported)
        {
            return LibrariesError.ImportedLibraryProtected;
        }

        // The books go with the library: their covers are deleted once the library is.
        var books = await bookRepository.ListByLibraryIdAsync(library.Id, cancellationToken);

        // Delete folders only for digital libraries
        if (library.BookType == LibraryBookType.Digital)
        {
            if (LibraryFolder.HasForeignFiles(libraryLocalStorage, library, books))
            {
                return LibrariesError.FolderHasForeignFiles;
            }

            var storageResult = libraryLocalStorage.Delete(library.RelativePath);
            if (storageResult.IsFailure)
            {
                return LibrariesError.FolderNotDeleted;
            }

            importDirectoryStorage.Delete(library.ImportDirectoryName);
        }

        librayRepository.Remove(library);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error!;
        }

        await coverStore.DeleteAsync(books, cancellationToken);

        return Result.Success();
    }
}
