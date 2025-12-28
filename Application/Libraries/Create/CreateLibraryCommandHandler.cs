using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Libraries.Create;

public sealed class CreateLibraryCommandHandler(IRepository<Library, Guid> libraryRepository, IUnitOfWork unitOfWork, ILibraryReadService libraryReadService, ILibraryLocalStorage libraryLocalStorage) : ICommandHandler<CreateLibraryCommand, Library>
{
    public async Task<Result<Library>> Handle(CreateLibraryCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        // Check if parameter are not null or empty
        if (string.IsNullOrEmpty(request.Name))
        {
            return LibrariesError.BadRequest;
        }

        // Check if a library with the same name doesn't already exist
        var pagedList = await libraryReadService.GetLibrariesAsync(request.Name, LibrariesColumn.Name, null, 1, 1, cancellationToken);
        Guard.Against.Null(pagedList);
        if (pagedList.TotalCount > 0)
        {
            return LibrariesError.Duplicate;
        }

        // Create Library
        var library = Library.Create(request.Name);

        // Create the directory for the library
        var result = libraryLocalStorage.Create(library.RelativePath);
        if (result.IsFailure)
        {
            return LibrariesError.FolderNotCreated;
        }

        libraryRepository.Add(library);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return library;
    }

}
