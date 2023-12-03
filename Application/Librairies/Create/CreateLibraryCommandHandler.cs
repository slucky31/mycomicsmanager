using MediatR;
using Domain.Libraries;
using Persistence.Primitives;
using Persistence;

namespace Application.Librairies.Create;

internal sealed class CreateLibraryCommandHandler : IRequestHandler<CreateLibraryCommand>
{
    private readonly IRepository<Library, LibraryId> _librayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLibraryCommandHandler(IRepository<Library, LibraryId> librayRepository, IUnitOfWork unitOfWork)
    {
        _librayRepository = librayRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CreateLibraryCommand request, CancellationToken cancellationToken)
    {
        var library = Library.Create(request.Name);
        
        _librayRepository.Add(library);
         
        await _unitOfWork.SaveChangesAsync(cancellationToken);                
    }
    
}
