using MediatR;
using Domain.Libraries;
using Domain.Primitives;
using Application.Data;
using Application.Interfaces;
using MongoDB.Bson;
using Ardalis.GuardClauses;
using Application.Libraries.ReadService;

namespace Application.Libraries.Create;

internal sealed class CreateLibraryCommandHandler : IRequestHandler<CreateLibraryCommand, Result<Library>>
{
    private readonly IRepository<Library, ObjectId> _libraryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILibraryReadService _libraryReadService;

    public CreateLibraryCommandHandler(IRepository<Library, ObjectId> libraryRepository, IUnitOfWork unitOfWork, ILibraryReadService libraryReadService)
    {
        _libraryRepository = libraryRepository;
        _unitOfWork = unitOfWork;
        _libraryReadService = libraryReadService;
    }

    public async Task<Result<Library>> Handle(CreateLibraryCommand command, CancellationToken cancellationToken)
    {
        // Check if parameter are not null or empty
        if (string.IsNullOrEmpty(command.Name))
        {
            return LibrariesErrors.BadRequest;
        }

        // Check if a library with the same name doesn't already exist
        var pagedList = await _libraryReadService.GetLibrariesAsync(command.Name, LibrariesColumn.Name, null, 1, 1);        
        Guard.Against.Null(pagedList);
        if ( pagedList.TotalCount > 0)
        {
            return LibrariesErrors.Duplicate;
        }

        // Create Library
        var library = Library.Create(command.Name);      
        _libraryRepository.Add(library);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return library;
    }
}
