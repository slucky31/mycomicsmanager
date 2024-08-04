using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using Application.Interfaces;
using MongoDB.Bson;
using Persistence.LocalStorage;

namespace Application.Libraries.Delete;
internal sealed class DeleteLibraryCommandHandler(IRepository<Library, ObjectId> librayRepository, IUnitOfWork unitOfWork, ILibraryLocalStorage libraryLocalStorage) : IRequestHandler<DeleteLibraryCommand, Result>
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
