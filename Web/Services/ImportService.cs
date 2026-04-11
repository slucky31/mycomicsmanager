using Application.Abstractions.Messaging;
using Application.ImportJobs;
using Application.ImportJobs.Create;
using Application.ImportJobs.GetById;
using Application.ImportJobs.List;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Primitives;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using Web.Models;

namespace Web.Services;

public class ImportService(
    IQueryHandler<ListImportJobsQuery, IReadOnlyList<ImportJob>> listImportJobsHandler,
    IQueryHandler<GetImportJobQuery, ImportJob> getImportJobHandler,
    ICommandHandler<CreateImportJobCommand, ImportJob> createImportJobHandler,
    IImportJobEnqueuer importJobEnqueuer,
    ICurrentUserService currentUserService,
    IOptions<ImportSettings> importSettings) : IImportService
{
    private readonly ImportSettings _settings = importSettings.Value;

    public async Task<Result<IReadOnlyList<ImportJobViewModel>>> GetImportJobsAsync(
        Guid libraryId, CancellationToken ct = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new ListImportJobsQuery(libraryId, userIdResult.Value);
        var result = await listImportJobsHandler.Handle(query, ct);

        if (result.IsFailure)
        {
            return result.Error!;
        }

        IReadOnlyList<ImportJobViewModel> viewModels = result.Value!
            .Select(ImportJobViewModel.From)
            .ToList();

        return Result<IReadOnlyList<ImportJobViewModel>>.Success(viewModels);
    }

    public async Task<Result<ImportJobViewModel>> GetImportJobAsync(
        Guid importJobId, CancellationToken ct = default)
    {
        var query = new GetImportJobQuery(importJobId);
        var result = await getImportJobHandler.Handle(query, ct);

        if (result.IsFailure)
        {
            return result.Error!;
        }

        return ImportJobViewModel.From(result.Value!);
    }

    public async Task<Result<ImportJobViewModel>> UploadAndCreateJobAsync(
        IBrowserFile file, Guid libraryId, CancellationToken ct = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync();
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var importDir = _settings.ImportDirectory;
        Directory.CreateDirectory(importDir);

        var safeFileName = Path.GetFileName(file.Name);
        var destPath = Path.Combine(importDir, $"{Guid.CreateVersion7()}_{safeFileName}");

        const long maxFileSizeBytes = 500L * 1024 * 1024;

        await using (var dest = File.OpenWrite(destPath))
        {
            await using var src = file.OpenReadStream(maxAllowedSize: maxFileSizeBytes, cancellationToken: ct);
            await src.CopyToAsync(dest, ct);
        }

        var fileInfo = new FileInfo(destPath);

        var command = new CreateImportJobCommand(
            OriginalFileName: safeFileName,
            OriginalFilePath: destPath,
            OriginalFileSize: fileInfo.Length,
            LibraryId: libraryId,
            UserId: userIdResult.Value);

        var createResult = await createImportJobHandler.Handle(command, ct);
        if (createResult.IsFailure)
        {
            // Clean up the uploaded file if job creation failed
            try
            { File.Delete(destPath); }
            catch (IOException) { /* best-effort cleanup */ }
            return createResult.Error!;
        }

        importJobEnqueuer.Enqueue(createResult.Value!.Id);

        return ImportJobViewModel.From(createResult.Value!);
    }
}
