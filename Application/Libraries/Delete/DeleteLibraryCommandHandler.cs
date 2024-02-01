using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using Application.Interfaces;
using MongoDB.Bson;

namespace Application.Libraries.Delete;
internal sealed class DeleteLibraryCommandHandler : IRequestHandler<DeleteLibraryCommand, Result>
{
    private readonly IRepository<Library, ObjectId> _librayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLibraryCommandHandler(IRepository<Library, ObjectId> librayRepository, IUnitOfWork unitOfWork)
    {
        _librayRepository = librayRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteLibraryCommand request, CancellationToken cancellationToken)
    {
        var library = await _librayRepository.GetByIdAsync(request.Id);

        if (library is null)
        {
            return LibrariesErrors.NotFound;
        }

        _librayRepository.Remove(library);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
