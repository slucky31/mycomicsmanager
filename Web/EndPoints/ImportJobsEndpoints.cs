using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.ImportJobs.DownloadFile;
using Application.Users;
using Domain.ImportJobs;

namespace Web.EndPoints;

internal static class ImportJobsEndpoints
{
    internal const string FailedFileContentType = "application/octet-stream";

    internal static void RegisterImportJobsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/import-jobs/{importJobId:guid}/file", DownloadFailedFileAsync).RequireAuthorization();
    }

    internal static async Task<IResult> DownloadFailedFileAsync(
        Guid importJobId,
        ClaimsPrincipal user,
        IQueryHandler<GetFailedImportFileQuery, FailedImportFile> handler,
        IUserReadService userReadService,
        CancellationToken ct)
    {
        var userId = await BooksEndpoints.ResolveUserIdAsync(user, userReadService, ct);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        // Ownership and the "failed" status are enforced by the query; the storage only returns
        // a path inside the import directory.
        var result = await handler.Handle(new GetFailedImportFileQuery(importJobId, userId.Value), ct);
        if (result.IsFailure || result.Value is null)
        {
            return result.Error == ImportJobError.BadRequest || result.Error == ImportJobError.NotFailed
                ? Results.BadRequest(result.Error?.Description)
                : Results.NotFound(result.Error?.Description);
        }

        var file = result.Value;
        return Results.File(file.FilePath, FailedFileContentType, file.FileName, enableRangeProcessing: true);
    }
}
