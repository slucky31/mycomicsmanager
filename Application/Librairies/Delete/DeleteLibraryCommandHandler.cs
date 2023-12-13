using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Librairies.Delete;
internal sealed class DeleteLibraryCommandHandler : IRequestHandler<DeleteLibraryCommand, Result>
{
    private readonly IRepository<Library, string> _librayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLibraryCommandHandler(IRepository<Library, string> librayRepository, IUnitOfWork unitOfWork)
    {
        _librayRepository = librayRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteLibraryCommand request, CancellationToken cancellationToken)
    {
        var library = await _librayRepository.GetByIdAsync(request.libraryId);

        if (library is null) 
        {
            return LibrariesErrors.NotFound;
        }

        _librayRepository.Remove(library);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
