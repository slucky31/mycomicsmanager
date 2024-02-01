using MediatR;
using Domain.Libraries;
using Domain.Primitives;
using Application.Data;
using Application.Interfaces;
using MongoDB.Bson;

namespace Application.Libraries.Create;

internal sealed class CreateLibraryCommandHandler : IRequestHandler<CreateLibraryCommand, Result<Library>>
{
    private readonly IRepository<Library, ObjectId> _libraryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLibraryCommandHandler(IRepository<Library, ObjectId> libraryRepository, IUnitOfWork unitOfWork)
    {
        _libraryRepository = libraryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Library>> Handle(CreateLibraryCommand command, CancellationToken cancellationToken)
    {
        // TODO : Test si Name est nul
        
        var library = Library.Create(command.Name);

        // TODO: Test uniqueness of the name

        _libraryRepository.Add(library);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return library;
    }
}
