using Application.ImportJobs;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Persistence.LocalStorage;

internal sealed class ImportDirectoryStorage : IImportDirectoryStorage
{
    private readonly string _rootPath;
    private static readonly char[] s_charsToTrim = ['/', '\\'];

    public ImportDirectoryStorage(IOptions<ImportSettings> settings)
    {
        _rootPath = settings.Value.ImportDirectory;
    }

    // Trailing separator ensures sibling paths like /data/import-other/ cannot
    // pass a bare StartsWith check against /data/import.
    private string NormalizedRootWithSeparator =>
        Path.GetFullPath(_rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        + Path.DirectorySeparatorChar;

    private Result ValidatePath(string directoryName)
    {
        try
        {
            var normalizedRoot = NormalizedRootWithSeparator;
            var fullPath = Path.GetFullPath(Path.Combine(_rootPath, directoryName));
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
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
            var normalizedRoot = NormalizedRootWithSeparator;
            var fullPath = Path.GetFullPath(absolutePath);
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
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
        if (!Directory.Exists(destinationPath))
        {
            Directory.Move(originPath, destinationPath);
        }

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

        var errorsDir = Path.Combine(_rootPath, "errors");
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
}
