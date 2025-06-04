using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
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

        if (library is null)
        {
            return LibrariesError.NotFound;
        }

        // Check if a library with the same name doesn't already exist
        var pagedList = await libraryReadService.GetLibrariesAsync(request.Name, LibrariesColumn.Name, null, 1, 1);
        Guard.Against.Null(pagedList);
        if (pagedList.TotalCount > 0)
        {
            return LibrariesError.Duplicate;
        }



        var originPath = library.RelativePath;

        library.Update(request.Name);



        var result = libraryLocalStorage.Move(originPath, library.RelativePath);

        if (result.IsFailure)

        {

            return LibrariesError.FolderNotMoved;

        }

        libraryRepository.Update(library);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return library;
    }
}
