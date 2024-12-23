using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using MongoDB.Bson;
using Persistence.LocalStorage;

namespace Application.Libraries.Update;

internal sealed class UpdateLibraryCommandHandler(IRepository<Library, ObjectId> librayRepository, IUnitOfWork unitOfWork, ILibraryReadService libraryReadService, ILibraryLocalStorage libraryLocalStorage) : IRequestHandler<UpdateLibraryCommand, Result<Library>>
{

    public async Task<Result<Library>> Handle(UpdateLibraryCommand request, CancellationToken cancellationToken)
    {
        var library = await librayRepository.GetByIdAsync(request.Id);

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

        librayRepository.Update(library);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return library;
    }
}
