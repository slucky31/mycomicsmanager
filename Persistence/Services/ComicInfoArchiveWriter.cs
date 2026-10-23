using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Application.Interfaces;
using Domain.Books;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Persistence.LocalStorage;

namespace Persistence.Services;

/// <summary>
/// Rewrites the ComicInfo.xml of a CBZ: the archive is copied to a temporary file next to it, with the new
/// ComicInfo.xml, then moved over the original, so a failure never leaves a half-written book.
/// </summary>
public sealed class ComicInfoArchiveWriter(string rootPath, ILogger<ComicInfoArchiveWriter> logger) : IComicInfoArchiveWriter
{
    private const string ComicInfoEntryName = "ComicInfo.xml";

    private static readonly HashSet<string> s_zipExtensions = new(StringComparer.OrdinalIgnoreCase) { ".cbz", ".zip" };

    public async Task<Result> WriteAsync(string archivePath, ComicInfoData data, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (string.IsNullOrWhiteSpace(archivePath) || !PathContainment.IsWithin(rootPath, archivePath))
        {
            logger.LogWarning("Archive outside of the library root refused: {Path}", archivePath);
            return FileProcessingError.InvalidPath;
        }

        if (!File.Exists(archivePath))
        {
            logger.LogWarning("Archive not found: {Path}", archivePath);
            return BooksError.FileNotFound;
        }

        if (!s_zipExtensions.Contains(Path.GetExtension(archivePath)))
        {
            logger.LogInformation("Not a CBZ, ComicInfo.xml left untouched: {Path}", archivePath);
            return Result.Success();
        }

        var tempPath = $"{archivePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await Task.Run(() => CopyWithComicInfo(archivePath, tempPath, data), ct);
            File.Move(tempPath, archivePath, overwrite: true);
            logger.LogInformation("ComicInfo.xml written into {Path}", archivePath);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or XmlException or ArgumentException)
        {
            logger.LogError(ex, "Failed to write ComicInfo.xml into {Path}", archivePath);
            TryDelete(tempPath);
            return FileProcessingError.XmlWriteError;
        }
        catch (OperationCanceledException)
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private void CopyWithComicInfo(string sourcePath, string targetPath, ComicInfoData data)
    {
        XDocument? existing = null;

        using var source = ZipFile.OpenRead(sourcePath);
        using var target = ZipFile.Open(targetPath, ZipArchiveMode.Create);
        foreach (var entry in source.Entries)
        {
            if (Path.GetFileName(entry.FullName).Equals(ComicInfoEntryName, StringComparison.OrdinalIgnoreCase))
            {
                existing ??= Load(entry, sourcePath);
                continue;
            }

            CopyEntry(entry, target);
        }

        var comicInfo = target.CreateEntry(ComicInfoEntryName, CompressionLevel.Fastest);
        using var stream = comicInfo.Open();
        Save(ComicInfoXmlMerger.Merge(existing, data), stream);
    }

    private static void CopyEntry(ZipArchiveEntry entry, ZipArchive target)
    {
        var copy = target.CreateEntry(entry.FullName, CompressionLevel.Fastest);
        copy.LastWriteTime = entry.LastWriteTime;
        using var from = entry.Open();
        using var to = copy.Open();
        from.CopyTo(to);
    }

    // An unreadable ComicInfo.xml is replaced by a new one rather than blocking the book.
    private XDocument? Load(ZipArchiveEntry entry, string archivePath)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = 1_000_000,
            // The document is indented again when saved.
            IgnoreWhitespace = true,
        };

        try
        {
            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, settings);
            return XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            logger.LogWarning(ex, "Unreadable ComicInfo.xml replaced in {Path}", archivePath);
            return null;
        }
    }

    private static void Save(XDocument document, Stream stream)
    {
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using var writer = XmlWriter.Create(stream, settings);
        document.Save(writer);
    }

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not delete the temporary archive {Path}", path);
        }
    }
}
