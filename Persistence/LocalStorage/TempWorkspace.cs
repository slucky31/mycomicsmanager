using Application.ImportJobs;
using Application.Interfaces;
using Application.Libraries;
using Domain.ImportJobs;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Persistence.LocalStorage;

internal sealed class TempWorkspace : ITempWorkspace
{
    private readonly string _tempDirectory;
    private readonly string _libraryRootPath;

    public TempWorkspace(IOptions<ImportSettings> settings, ILibraryLocalStorage libraryStorage)
    {
        _tempDirectory = settings.Value.TempDirectory;
        _libraryRootPath = libraryStorage.rootPath;
    }

    public bool HasFreeSpace(long requiredBytes)
    {
        try
        {
            var driveRoot = Path.GetPathRoot(_tempDirectory) ?? "/";
            return new DriveInfo(driveRoot).AvailableFreeSpace >= requiredBytes;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Serilog.Log.Warning(ex, "Could not check disk space for {TempDir}, proceeding anyway", _tempDirectory);
            return true;
        }
    }

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
        return [.. Directory.GetFiles(directory, "*.webp").OrderBy(f => f, StringComparer.OrdinalIgnoreCase)];
    }

    public Result<string> MoveToLibrary(string sourcePath, string libraryRelativePath, string fileName)
    {
        var libraryDir = Path.Combine(_libraryRootPath, libraryRelativePath);
        Directory.CreateDirectory(libraryDir);
        var finalPath = Path.Combine(libraryDir, fileName);
        var normalizedLibraryDir = Path.GetFullPath(libraryDir)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(finalPath).StartsWith(normalizedLibraryDir, StringComparison.OrdinalIgnoreCase))
        {
            return ImportJobError.BadRequest;
        }
        File.Move(sourcePath, finalPath, overwrite: true);
        return finalPath;
    }

    public void TryDeleteFile(string path)
    {
        try
        { File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Serilog.Log.Error(ex, "Compensation failed: could not delete {Path}", path);
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
        catch (IOException ex) { Serilog.Log.Warning(ex, "Could not delete temp directory {Dir}", dir); }
        catch (UnauthorizedAccessException ex) { Serilog.Log.Warning(ex, "Could not delete temp directory {Dir}", dir); }
    }
}
