using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using Application.Interfaces;
using MongoDB.Bson;

namespace Application.Libraries.Update;

internal sealed class UpdateLibraryCommandHandler(IRepository<Library, ObjectId> librayRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateLibraryCommand, Result>
{
    public async Task<Result> Handle(UpdateLibraryCommand request, CancellationToken cancellationToken)
    {
        var library = await librayRepository.GetByIdAsync(request.Id);

        if (library is null)
        {
            return LibrariesError.NotFound;
        }

        library.Update(request.Name);
        
        librayRepository.Update(library);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
