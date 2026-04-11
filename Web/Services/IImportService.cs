using Domain.Primitives;
using Microsoft.AspNetCore.Components.Forms;
using Web.Models;

namespace Web.Services;

public interface IImportService
{
    Task<Result<IReadOnlyList<ImportJobViewModel>>> GetImportJobsAsync(Guid libraryId, CancellationToken ct = default);
    Task<Result<ImportJobViewModel>> GetImportJobAsync(Guid importJobId, CancellationToken ct = default);
    Task<Result<ImportJobViewModel>> UploadAndCreateJobAsync(IBrowserFile file, Guid libraryId, CancellationToken ct = default);
}
