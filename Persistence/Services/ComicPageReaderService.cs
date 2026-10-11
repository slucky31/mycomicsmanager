using Application.Interfaces;
using Domain.Books;
using Domain.Errors;
using Domain.Primitives;
using Microsoft.Extensions.Logging;
using Persistence.LocalStorage;
using SharpCompress.Archives;
using SharpCompress.Readers;

namespace Persistence.Services;

/// <summary>
/// Serves comic pages straight from the archive: each page is decompressed into memory
/// for the duration of the request, so reading a book never writes a file on disk.
/// </summary>
public sealed class ComicPageReaderService(string rootPath, ILogger<ComicPageReaderService> logger) : IComicPageReader
{
    private static readonly Dictionary<string, string> s_contentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".webp"] = "image/webp",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
    };

    private const long MaxPageBytes = 50L * 1024 * 1024; // 50 MB

    public Task<Result<int>> CountPagesAsync(string archivePath, CancellationToken ct = default) =>
        RunOnArchiveAsync(archivePath, archive => Result<int>.Success(GetPageEntries(archive).Count), ct);

    public Task<Result<ComicPage>> ReadPageAsync(string archivePath, int pageIndex, CancellationToken ct = default)
    {
        if (pageIndex < 0)
        {
            return Task.FromResult<Result<ComicPage>>(BooksError.PageNotFound);
        }

        return RunOnArchiveAsync(archivePath, archive => ReadPage(archive, archivePath, pageIndex), ct);
    }

    private async Task<Result<T>> RunOnArchiveAsync<T>(string archivePath, Func<IArchive, Result<T>> action, CancellationToken ct)
    {
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

        try
        {
            return await Task.Run(() =>
            {
                using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var archive = ArchiveFactory.OpenArchive(fileStream, new ReaderOptions { LookForHeader = true });
                return action(archive);
            }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to read archive: {Path}", archivePath);
            return FileProcessingError.CorruptArchive;
        }
    }

    private static List<IArchiveEntry> GetPageEntries(IArchive archive) =>
        [.. archive.Entries
            .Where(e => !e.IsDirectory && s_contentTypes.ContainsKey(Path.GetExtension(e.Key ?? string.Empty)))
            .OrderBy(e => e.Key, NaturalStringComparer.Instance)];

    private Result<ComicPage> ReadPage(IArchive archive, string archivePath, int pageIndex)
    {
        var entries = GetPageEntries(archive);
        if (pageIndex >= entries.Count)
        {
            return BooksError.PageNotFound;
        }

        var entry = entries[pageIndex];
        if (entry.Size > MaxPageBytes)
        {
            logger.LogWarning("Page {Index} of {Path} exceeds the size limit ({Size} bytes)", pageIndex, archivePath, entry.Size);
            return FileProcessingError.CorruptArchive;
        }

        using var entryStream = entry.OpenEntryStream();
        using var buffer = new MemoryStream((int)Math.Max(entry.Size, 0));
        entryStream.CopyTo(buffer);

        var contentType = s_contentTypes[Path.GetExtension(entry.Key!)];
        // Hands over the stream's own buffer instead of copying it a second time.
        var content = new ReadOnlyMemory<byte>(buffer.GetBuffer(), 0, (int)buffer.Length);
        return new ComicPage(content, contentType, File.GetLastWriteTimeUtc(archivePath));
    }
}
