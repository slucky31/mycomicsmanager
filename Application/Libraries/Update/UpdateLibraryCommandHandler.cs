using Application.Data;
using Domain.Libraries;
using Domain.Primitives;
using MediatR;
using Application.Interfaces;
using Domain.Dto;

namespace Application.Libraries.Update;

internal sealed class UpdateLibraryCommandHandler : IRequestHandler<UpdateLibraryCommand, Result>
{
    private readonly IRepository<LibraryDto, LibraryId> _librayRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLibraryCommandHandler(IRepository<LibraryDto, LibraryId> librayRepository, IUnitOfWork unitOfWork)
    {
        _librayRepository = librayRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateLibraryCommand request, CancellationToken cancellationToken)
    {
        var libraryDto = await _librayRepository.GetByIdAsync(request.Id);

        if (libraryDto is null)
        {
            return LibrariesErrors.NotFound;
        }

        libraryDto.Update(request.Name);
        
        _librayRepository.Update(libraryDto);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
