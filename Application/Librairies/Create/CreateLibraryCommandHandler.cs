using MediatR;
using Domain.Libraries;
using Domain.Primitives;
using Application.Data;

namespace Application.Librairies.Create;

internal sealed class CreateLibraryCommandHandler : IRequestHandler<CreateLibraryCommand, Result<Library>>
{
    private readonly IRepository<Library, string> _librayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLibraryCommandHandler(IRepository<Library, string> librayRepository, IUnitOfWork unitOfWork)
    {
        _librayRepository = librayRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Library>> Handle(CreateLibraryCommand command, CancellationToken cancellationToken)
    {
        var library = Library.Create(command.Name);
        
        _librayRepository.Add(library);
         
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return library;        
    }
    
}
