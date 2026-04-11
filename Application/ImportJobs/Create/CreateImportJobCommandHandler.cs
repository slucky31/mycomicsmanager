using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.ImportJobs.Create;

public sealed class CreateImportJobCommandHandler(
    IImportJobRepository importJobRepository,
    IRepository<Library, Guid> libraryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateImportJobCommand, ImportJob>
{
    private static readonly HashSet<string> s_allowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".cbz", ".cbr", ".zip", ".rar", ".pdf" };

    public async Task<Result<ImportJob>> Handle(CreateImportJobCommand request, CancellationToken cancellationToken)
    {
        // Validate input parameters
        if (string.IsNullOrWhiteSpace(request.OriginalFileName) ||
            string.IsNullOrWhiteSpace(request.OriginalFilePath) ||
            request.OriginalFileSize <= 0 ||
            request.LibraryId == Guid.Empty)
        {
            return ImportJobError.BadRequest;
        }

        // Validate file extension
        var extension = Path.GetExtension(request.OriginalFileName);
        if (!s_allowedExtensions.Contains(extension))
        {
            return ImportJobError.BadRequest;
        }

        // Verify library exists and belongs to user
        var library = await libraryRepository.GetByIdAsync(request.LibraryId);
        if (library is null)
        {
            return LibrariesError.NotFound;
        }

        if (library.UserId != request.UserId)
        {
            return LibrariesError.NotFound;
        }

        // Verify library is of type Digital
        if (library.BookType != LibraryBookType.Digital)
        {
            return LibrariesError.BookTypeMismatch;
        }

        // Create the ImportJob
        var createResult = ImportJob.Create(
            request.OriginalFileName,
            request.OriginalFilePath,
            request.OriginalFileSize,
            request.LibraryId);

        if (createResult.IsFailure)
        {
            return createResult.Error!;
        }

        var importJob = createResult.Value!;

        importJobRepository.Add(importJob);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return importJob;
    }
}
