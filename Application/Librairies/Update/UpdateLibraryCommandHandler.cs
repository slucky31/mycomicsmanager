using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;

namespace Application.Librairies.Update;

internal sealed class UpdateLibraryCommandHandler : IRequestHandler<UpdateLibraryCommand>
{
    private readonly IRepository<Library, LibraryId> _librayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLibraryCommandHandler(IRepository<Library, LibraryId> librayRepository, IUnitOfWork unitOfWork)
    {
        _librayRepository = librayRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateLibraryCommand request, CancellationToken cancellationToken)
    {
        var library = await _librayRepository.GetByIdAsync(request.libraryId);

        if (library is null)
        {
            throw new LibraryNotFoundException(request.libraryId);
        }

        library.Update(request.Name);
        
        _librayRepository.Update(library);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
