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

    private Result ValidatePath(string directoryName)
    {
        try
        {
            var normalizedRoot = Path.GetFullPath(_rootPath);
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
}
