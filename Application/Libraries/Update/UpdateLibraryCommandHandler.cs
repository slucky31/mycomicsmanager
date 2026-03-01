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

        // Update name only if provided
        if (request.Name is not null)
        {
            // Check for duplicate name within same user (excluding this library)
            if (await libraryReadService.ExistsByNameAsync(request.Name, request.UserId, request.Id, cancellationToken))
            {
                return LibrariesError.Duplicate;
            }

            var originPath = library.RelativePath;

            var updateNameResult = library.UpdateName(request.Name);
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
        }

        libraryRepository.Update(library);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return library;
    }
}
