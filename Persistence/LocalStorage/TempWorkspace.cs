using Application.ImportJobs;
using Application.Interfaces;
using Application.Libraries;
using Domain.ImportJobs;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Services;

namespace Persistence.LocalStorage;

internal sealed class TempWorkspace : ITempWorkspace
{
    private readonly string _tempDirectory;
    private readonly string _libraryRootPath;
    private readonly ILogger<TempWorkspace> _logger;

    public TempWorkspace(IOptions<ImportSettings> settings, ILibraryLocalStorage libraryStorage, ILogger<TempWorkspace> logger)
    {
        _logger = logger;
        _tempDirectory = settings.Value.TempDirectory;
        _libraryRootPath = libraryStorage.rootPath;
    }

    // Measured on the temp directory itself: it may be a volume of its own, not the one of "/" (#1370).
    public bool HasFreeSpace(long requiredBytes) => DiskSpace.AvailableBytes(_tempDirectory) >= requiredBytes;

    public (string TempDir, string RawDir, string ConvertedDir) CreateScratch(Guid jobId)
    {
        var tempDir = Path.Combine(_tempDirectory, jobId.ToString());
        return (tempDir, Path.Combine(tempDir, "raw"), Path.Combine(tempDir, "converted"));
    }

    public IReadOnlyList<string> GetWebpFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }
        return [.. Directory.GetFiles(directory, "*.webp").Order(StringComparer.OrdinalIgnoreCase)];
    }

    public Result<string> MoveToLibrary(string sourcePath, string libraryRelativePath, string fileName)
    {
        var libraryDir = Path.Combine(_libraryRootPath, libraryRelativePath);
        var finalPath = Path.Combine(libraryDir, fileName);
        if (!PathContainment.IsWithin(_libraryRootPath, finalPath))
        {
            return ImportJobError.BadRequest;
        }
        Directory.CreateDirectory(libraryDir);
        if (File.Exists(finalPath))
        {
            return ImportJobError.DestinationFileExists;
        }
        File.Move(sourcePath, finalPath, overwrite: false);
        return finalPath;
    }

    public void TryDeleteFile(string path)
    {
        try
        { File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Compensation failed: could not delete {Path}", path);
        }
    }

    public void CleanupDirectory(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }
        try
        { Directory.Delete(dir, true); }
        catch (IOException ex) { _logger.LogWarning(ex, "Could not delete temp directory {Dir}", dir); }
        catch (UnauthorizedAccessException ex) { _logger.LogWarning(ex, "Could not delete temp directory {Dir}", dir); }
    }
}
