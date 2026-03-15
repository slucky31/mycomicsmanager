using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.Update;

public sealed class UpdateLibraryCommandHandler(IRepository<Library, Guid> libraryRepository, IUnitOfWork unitOfWork, ILibraryReadService libraryReadService, ILibraryLocalStorage libraryLocalStorage) : ICommandHandler<UpdateLibraryCommand, Library>
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
            var nameResult = await ApplyNameUpdateAsync(library, request.Name, request.UserId, cancellationToken);
            if (nameResult.IsFailure)
            {
                return nameResult.Error!;
            }
        }

        libraryRepository.Update(library);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return library;
    }

    private async Task<Result> ApplyNameUpdateAsync(Library library, string name, Guid userId, CancellationToken cancellationToken)
    {
        if (await libraryReadService.ExistsByNameAsync(name, userId, library.Id, cancellationToken))
        {
            return LibrariesError.Duplicate;
        }

        var originPath = library.RelativePath;

        var updateNameResult = library.UpdateName(name);
        if (updateNameResult.IsFailure)
        {
            return updateNameResult.Error!;
        }

        // Move folder only for digital libraries
        if (library.BookType == LibraryBookType.Digital)
        {
            var moveResult = libraryLocalStorage.Move(originPath, library.RelativePath);
            if (moveResult.IsFailure)
            {
                return LibrariesError.FolderNotMoved;
            }
        }

        return Result.Success();
    }
}
