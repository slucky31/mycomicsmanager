using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Persistence.Services;

public sealed partial class BookFileService(ILogger<BookFileService> logger) : IBookFileService
{
    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.CompletedTask;
        }

        if (!File.Exists(filePath))
        {
            LogFileNotFound(logger, filePath);
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(filePath);
            LogFileDeleted(logger, filePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFileDeletionFailed(logger, ex, filePath);
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Digital book file not found, skipping deletion: {FilePath}")]
    private static partial void LogFileNotFound(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted digital book file: {FilePath}")]
    private static partial void LogFileDeleted(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete digital book file: {FilePath}")]
    private static partial void LogFileDeletionFailed(ILogger logger, Exception ex, string filePath);
}
