using MediatR;
using Domain.Libraries;
using Domain.Primitives;
using Application.Data;
using Application.Interfaces;
using MongoDB.Bson;
using Ardalis.GuardClauses;
using Persistence.LocalStorage;

namespace Application.Libraries.Create;

internal sealed class CreateLibraryCommandHandler(IRepository<Library, ObjectId> libraryRepository, IUnitOfWork unitOfWork, ILibraryReadService libraryReadService, ILibraryLocalStorage libraryLocalStorage) : IRequestHandler<CreateLibraryCommand, Result<Library>>
{
    public async Task<Result<Library>> Handle(CreateLibraryCommand command, CancellationToken cancellationToken)
    {
        // Check if parameter are not null or empty
        if (string.IsNullOrEmpty(command.Name))
        {
            return LibrariesError.BadRequest;
        }

        // Check if a library with the same name doesn't already exist
        var pagedList = await libraryReadService.GetLibrariesAsync(command.Name, LibrariesColumn.Name, null, 1, 1);        
        Guard.Against.Null(pagedList);
        if ( pagedList.TotalCount > 0)
        {
            return LibrariesError.Duplicate;
        }
        
        // Create Library
        var library = Library.Create(command.Name);

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
