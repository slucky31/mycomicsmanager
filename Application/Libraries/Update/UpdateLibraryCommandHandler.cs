using Application.Abstractions.Messaging;
using Application.ImportJobs;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.Update;

public sealed class UpdateLibraryCommandHandler(IRepository<Library, Guid> libraryRepository, IUnitOfWork unitOfWork, ILibraryReadService libraryReadService, ILibraryLocalStorage libraryLocalStorage, IImportDirectoryStorage importDirectoryStorage, IBookRepository bookRepository) : ICommandHandler<UpdateLibraryCommand, Library>
{
    public async Task<Result<Library>> Handle(UpdateLibraryCommand request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return LibrariesError.ValidationError;
        }

        var library = await libraryRepository.GetByIdAsync(request.Id);

        if (library is null || library.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        // Update color and icon
        var updateResult = library.Update(request.Color, request.Icon);
        if (updateResult.IsFailure)
        {
            return updateResult.Error!;
        }

        // Validate and apply sort order before any storage side-effects
        if (request.DefaultBookSortOrder is not null)
        {
            var sortResult = library.UpdateDefaultBookSortOrder(request.DefaultBookSortOrder.Value);
            if (sortResult.IsFailure)
            {
                return sortResult.Error!;
            }
        }

        // Update name only if provided
        if (request.Name is not null)
        {
            var nameResult = await ApplyNameUpdateAsync(library, request.Id, request.Name, request.UserId, cancellationToken);
            if (nameResult.IsFailure)
            {
                return nameResult.Error!;
            }
        }

        libraryRepository.Update(library);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error!;
        }

        return library;
    }

    private async Task<Result> ApplyNameUpdateAsync(Library library, Guid libraryId, string name, Guid userId, CancellationToken cancellationToken)
    {
        // The edit form always sends the name: unchanged, nothing is renamed or moved.
        if (string.Equals(name, library.Name, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        if (library.IsImported)
        {
            return LibrariesError.ImportedLibraryProtected;
        }

        if (await libraryReadService.ExistsByNameAsync(name, userId, libraryId, cancellationToken))
        {
            return LibrariesError.Duplicate;
        }

        var originPath = library.RelativePath;
        var originImportDirName = library.ImportDirectoryName;

        var updateNameResult = library.UpdateName(name);
        if (updateNameResult.IsFailure)
        {
            return updateNameResult.Error!;
        }

        // Move folders only for digital libraries whose folder name changes (not for a change of case or accents)
        if (library.BookType != LibraryBookType.Digital || originPath == library.RelativePath)
        {
            return Result.Success();
        }

        // The files keep their place in the folder: their new paths are set before the folder is moved.
        var relocated = await RelocateBooksAsync(library, originPath, cancellationToken);
        if (relocated.IsFailure)
        {
            return relocated;
        }

        var moveResult = libraryLocalStorage.Move(originPath, library.RelativePath);
        if (moveResult.IsFailure)
        {
            return LibrariesError.FolderNotMoved;
        }

        var importMoveResult = importDirectoryStorage.Move(originImportDirName, library.ImportDirectoryName);
        if (importMoveResult.IsFailure)
        {
            libraryLocalStorage.Move(library.RelativePath, originPath);
            return LibrariesError.FolderNotMoved;
        }

        return Result.Success();
    }

    private async Task<Result> RelocateBooksAsync(Library library, string originPath, CancellationToken cancellationToken)
    {
        var books = await bookRepository.ListByLibraryIdAsync(library.Id, cancellationToken);
        foreach (var book in books.OfType<DigitalBook>())
        {
            var filePath = libraryLocalStorage.PathAfterMove(book.FilePath, originPath, library.RelativePath);
            if (filePath is null)
            {
                continue;
            }

            var relocated = book.Relocate(filePath);
            if (relocated.IsFailure)
            {
                return relocated;
            }
        }

        return Result.Success();
    }
}
