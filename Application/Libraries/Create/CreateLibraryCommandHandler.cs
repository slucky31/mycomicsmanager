using MediatR;
using Domain.Libraries;
using Domain.Primitives;
using Application.Data;
using Application.Interfaces;
using Domain.Dto;

namespace Application.Libraries.Create;

internal sealed class CreateLibraryCommandHandler : IRequestHandler<CreateLibraryCommand, Result<Library>>
{
    private readonly IRepository<LibraryDto, LibraryId> _libraryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLibraryCommandHandler(IRepository<LibraryDto, LibraryId> libraryRepository, IUnitOfWork unitOfWork)
    {
        _libraryRepository = libraryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Library>> Handle(CreateLibraryCommand command, CancellationToken cancellationToken)
    {
        // TODO : Test si Name est nul
        
        var libraryDto = LibraryDto.Create(Library.Create(command.Name));

        // TODO: Test uniqueness of the name

        _libraryRepository.Add(libraryDto);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return libraryDto.ToLibrary();
    }
}
