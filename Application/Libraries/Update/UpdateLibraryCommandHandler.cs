using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using Application.Interfaces;
using MongoDB.Bson;

namespace Application.Libraries.Update;

internal sealed class UpdateLibraryCommandHandler : IRequestHandler<UpdateLibraryCommand, Result>
{
    private readonly IRepository<Library, ObjectId> _librayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLibraryCommandHandler(IRepository<Library, ObjectId> librayRepository, IUnitOfWork unitOfWork)
    {
        _librayRepository = librayRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateLibraryCommand request, CancellationToken cancellationToken)
    {
        var library = await _librayRepository.GetByIdAsync(request.Id);

        if (library is null)
        {
            return LibrariesErrors.NotFound;
        }

        library.Update(request.Name, request.relPath);
        
        _librayRepository.Update(library);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
