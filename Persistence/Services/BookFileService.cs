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
            LogFileNotFound(filePath);
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(filePath);
            LogFileDeleted(filePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFileDeletionFailed(ex, filePath);
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Digital book file not found, skipping deletion: {FilePath}")]
    private partial void LogFileNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted digital book file: {FilePath}")]
    private partial void LogFileDeleted(string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to delete digital book file: {FilePath}")]
    private partial void LogFileDeletionFailed(Exception ex, string filePath);
}
