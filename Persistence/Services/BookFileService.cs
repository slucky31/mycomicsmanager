using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Persistence.Services;

public sealed class BookFileService(ILogger<BookFileService> logger) : IBookFileService
{
    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.CompletedTask;
        }

        if (!File.Exists(filePath))
        {
            logger.LogWarning("Digital book file not found, skipping deletion: {FilePath}", filePath);
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(filePath);
            logger.LogInformation("Deleted digital book file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete digital book file: {FilePath}", filePath);
        }

        return Task.CompletedTask;
    }
}
