using Application.ImportJobs;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Persistence.LocalStorage;

internal sealed class ImportDirectoryStorage : IImportDirectoryStorage
{
    private readonly string _rootPath;
    private static readonly char[] s_charsToTrim = ['/', '\\'];
    private const string PartialFileExtension = ".part";

    public ImportDirectoryStorage(IOptions<ImportSettings> settings)
    {
        _rootPath = settings.Value.ImportDirectory;
    }

    private Result ValidatePath(string directoryName)
    {
        try
        {
            var fullPath = Path.GetFullPath(Path.Combine(_rootPath, directoryName));
            if (!PathContainment.IsWithin(_rootPath, fullPath))
            {
                return ImportDirectoryStorageError.InvalidPath;
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ImportDirectoryStorageError.InvalidPath;
        }
    }

    private Result ValidateAbsolutePath(string absolutePath)
    {
        try
        {
            if (!PathContainment.IsWithin(_rootPath, absolutePath))
            {
                return ImportDirectoryStorageError.InvalidPath;
            }

            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ImportDirectoryStorageError.InvalidPath;
        }
    }

    public Result EnsureExists(string directoryName)
    {
        if (string.IsNullOrEmpty(directoryName))
        {
            return ImportDirectoryStorageError.ArgumentNullOrEmpty;
        }

        var pathValidation = ValidatePath(directoryName);
        if (pathValidation.IsFailure)
        {
            return pathValidation.Error!;
        }

        var path = Path.Combine(_rootPath.TrimEnd(s_charsToTrim), directoryName);
        Directory.CreateDirectory(path);
        return Result.Success();
    }

    public Result Move(string originDirectoryName, string destinationDirectoryName)
    {
        if (string.IsNullOrEmpty(originDirectoryName) || string.IsNullOrEmpty(destinationDirectoryName))
        {
            return ImportDirectoryStorageError.ArgumentNullOrEmpty;
        }

        var originValidation = ValidatePath(originDirectoryName);
        if (originValidation.IsFailure)
        {
            return originValidation.Error!;
        }

        var destinationValidation = ValidatePath(destinationDirectoryName);
        if (destinationValidation.IsFailure)
        {
            return destinationValidation.Error!;
        }

        var originPath = Path.Combine(_rootPath.TrimEnd(s_charsToTrim), originDirectoryName);
        if (!Directory.Exists(originPath))
        {
            return Result.Success();
        }

        var destinationPath = Path.Combine(_rootPath.TrimEnd(s_charsToTrim), destinationDirectoryName);
        if (Directory.Exists(destinationPath))
        {
            return ImportDirectoryStorageError.DestinationAlreadyExists;
        }

        Directory.Move(originPath, destinationPath);
        return Result.Success();
    }

    public Result Delete(string directoryName)
    {
        if (string.IsNullOrEmpty(directoryName))
        {
            return ImportDirectoryStorageError.ArgumentNullOrEmpty;
        }

        var pathValidation = ValidatePath(directoryName);
        if (pathValidation.IsFailure)
        {
            return pathValidation.Error!;
        }

        var path = Path.Combine(_rootPath.TrimEnd(s_charsToTrim), directoryName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        return Result.Success();
    }

    public Result DeleteOriginalFile(string absoluteFilePath)
    {
        if (string.IsNullOrEmpty(absoluteFilePath))
        {
            return ImportDirectoryStorageError.ArgumentNullOrEmpty;
        }

        var validation = ValidateAbsolutePath(absoluteFilePath);
        if (validation.IsFailure)
        {
            return validation.Error!;
        }

        if (File.Exists(absoluteFilePath))
        {
            File.Delete(absoluteFilePath);
        }

        return Result.Success();
    }

    public Result MoveOriginalFileToError(string absoluteFilePath)
    {
        if (string.IsNullOrEmpty(absoluteFilePath))
        {
            return ImportDirectoryStorageError.ArgumentNullOrEmpty;
        }

        var validation = ValidateAbsolutePath(absoluteFilePath);
        if (validation.IsFailure)
        {
            return validation.Error!;
        }

        if (!File.Exists(absoluteFilePath))
        {
            return Result.Success();
        }

        var libraryDir = Path.GetDirectoryName(absoluteFilePath) ?? _rootPath;
        var errorsDir = Path.Combine(libraryDir, "errors");
        Directory.CreateDirectory(errorsDir);

        var fileName = Path.GetFileName(absoluteFilePath);
        var destPath = Path.Combine(errorsDir, fileName);

        if (File.Exists(destPath))
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            var ext = Path.GetExtension(fileName);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            destPath = Path.Combine(errorsDir, $"{nameWithoutExt}_{timestamp}{ext}");
        }

        File.Move(absoluteFilePath, destPath);
        return Result.Success();
    }

    public Result<string> GetAvailableFilePath(string directoryName, string fileName)
    {
        if (string.IsNullOrEmpty(directoryName))
        {
            return ImportDirectoryStorageError.ArgumentNullOrEmpty;
        }

        var safeName = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(safeName) || safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return ImportDirectoryStorageError.InvalidFileName;
        }

        var pathValidation = ValidatePath(directoryName);
        if (pathValidation.IsFailure)
        {
            return pathValidation.Error!;
        }

        var directory = Path.GetFullPath(Path.Combine(_rootPath, directoryName));
        var path = Path.Combine(directory, safeName);
        var suffix = 2;
        while (File.Exists(path) || File.Exists(path + PartialFileExtension))
        {
            var candidate = $"{Path.GetFileNameWithoutExtension(safeName)} ({suffix}){Path.GetExtension(safeName)}";
            path = Path.Combine(directory, candidate);
            suffix++;
        }

        var fileValidation = ValidateAbsolutePath(path);
        return fileValidation.IsFailure ? fileValidation.Error! : path;
    }

    public Result DepositFile(string sourceFilePath, string destinationFilePath)
    {
        if (string.IsNullOrEmpty(sourceFilePath) || string.IsNullOrEmpty(destinationFilePath))
        {
            return ImportDirectoryStorageError.ArgumentNullOrEmpty;
        }

        var validation = ValidateAbsolutePath(destinationFilePath);
        if (validation.IsFailure)
        {
            return validation.Error!;
        }

        if (!File.Exists(sourceFilePath))
        {
            return ImportDirectoryStorageError.SourceFileNotFound;
        }

        if (File.Exists(destinationFilePath))
        {
            return ImportDirectoryStorageError.FileAlreadyExists;
        }

        // The temp directory may be on another volume: the (non atomic) copy goes to a ".part" file the watcher
        // ignores, then the rename inside the import directory is atomic.
        var partialPath = destinationFilePath + PartialFileExtension;
        try
        {
            File.Move(sourceFilePath, partialPath, overwrite: true);
            File.Move(partialPath, destinationFilePath);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (File.Exists(partialPath))
            {
                File.Delete(partialPath);
            }

            return ImportDirectoryStorageError.DepositFailed;
        }
    }
}
