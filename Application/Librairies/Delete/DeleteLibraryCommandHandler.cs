using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Librairies.Delete;
internal sealed class DeleteLibraryCommandHandler : IRequestHandler<DeleteLibraryCommand>
{
    private readonly IRepository<Library, LibraryId> _librayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLibraryCommandHandler(IRepository<Library, LibraryId> librayRepository, IUnitOfWork unitOfWork)
    {
        _librayRepository = librayRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteLibraryCommand request, CancellationToken cancellationToken)
    {
        var library = await _librayRepository.GetByIdAsync(request.libraryId);

        if (library is null) 
        {
            throw new LibraryNotFoundException(request.libraryId);
        }

        _librayRepository.Remove(library);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
