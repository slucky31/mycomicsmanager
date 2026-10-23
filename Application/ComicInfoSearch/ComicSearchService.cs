using System.Text.RegularExpressions;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

public partial class ComicSearchService : IComicSearchService
{
    private readonly IBnfCatalogueService _bnfCatalogueService;
    private readonly IOpenLibraryService _openLibraryService;
    private readonly IGoogleBooksService _googleBooksService;
    private readonly IBedethequeService _bedethequeService;
    private readonly IBubbleService _bubbleService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly CloudinarySettings _cloudinarySettings;
    private readonly ILogger<ComicSearchService> _logger;

    public ComicSearchService(
        BookCatalogues catalogues,
        ICloudinaryService cloudinaryService,
        IOptions<CloudinarySettings> cloudinarySettings,
        ILogger<ComicSearchService> logger)
    {
        _logger = logger;
        _bnfCatalogueService = catalogues.Bnf;
        _openLibraryService = catalogues.OpenLibrary;
        _googleBooksService = catalogues.GoogleBooks;
        _bedethequeService = catalogues.Bedetheque;
        _bubbleService = catalogues.Bubble;
        _cloudinaryService = cloudinaryService;
        _cloudinarySettings = cloudinarySettings.Value;
    }

    public async Task<ComicSearchResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var cleanIsbn = isbn.Replace("-", "", StringComparison.Ordinal)
                               .Replace(" ", "", StringComparison.Ordinal)
                               .Trim();

        try
        {
            var match = await FindFirstAsync(cleanIsbn, cancellationToken);
            if (match is null)
            {
                return CreateNotFoundResult(cleanIsbn);
            }

            var imageUrl = match.CoverUrl != null
                ? await UploadCoverToCloudinaryAsync(match.CoverUrl, cleanIsbn, cancellationToken)
                : string.Empty;
            return match.Metadata with { ImageUrl = imageUrl };
        }
        catch (Exception ex) when (IsUnexpectedException(ex, cancellationToken))
        {
            _logger.LogError(ex, "Unexpected error searching for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
    }

    public (string Title, string Serie, int? VolumeNumber) ParseTitleInfo(IBookSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        // The source gives the series apart: the title is the one of the volume.
        if (!string.IsNullOrWhiteSpace(result.Serie))
        {
            return (result.Title, result.Serie, result.VolumeNumber);
        }

        var (serie, volumeNumber) = ParseVolumeAndSerie(result.Title);
        if (volumeNumber is null && NumberedTitlePattern().Match(result.Title) is { Success: true } numbered)
        {
            return (numbered.Groups["title"].Value.Trim(), numbered.Groups["serie"].Value.Trim(), SeriesHelper.ParseVolumeNumber(numbered.Groups["volume"].Value));
        }

        var title = string.IsNullOrEmpty(result.Subtitle) ? serie : result.Subtitle;
        return (title, serie, volumeNumber);
    }

    public Task<string> UploadCoverAsync(Uri coverUrl, string isbn, CancellationToken cancellationToken = default)
        => UploadCoverToCloudinaryAsync(coverUrl, isbn, cancellationToken);

    public async Task<ComicSearchResult> SearchByIsbnWithLocalCoverAsync(
        string isbn,
        Stream? coverStream,
        string? coverFileName,
        CancellationToken cancellationToken = default)
    {
        var cleanIsbn = isbn.Replace("-", "", StringComparison.Ordinal)
                           .Replace(" ", "", StringComparison.Ordinal)
                           .Trim();

        if (!string.IsNullOrEmpty(cleanIsbn))
        {
            var match = await FindFirstAsync(cleanIsbn, cancellationToken);
            if (match is not null)
            {
                var imageUrl = await UploadCoverStreamOrRemoteAsync(
                    coverStream, coverFileName, match.CoverUrl, cleanIsbn, cancellationToken);
                return match.Metadata with { ImageUrl = imageUrl };
            }
        }

        // No metadata found – upload local cover only (guid-based publicId when no ISBN)
        var coverImageUrl = coverStream != null && coverFileName != null
            ? await UploadLocalCoverAsync(coverStream, coverFileName, cleanIsbn, cancellationToken)
            : string.Empty;

        return CreateNotFoundResult(cleanIsbn) with { ImageUrl = coverImageUrl };
    }

    // The metadata of the first source that knows the book (its ImageUrl not set yet) and the cover to upload.
    private sealed record ProviderMatch(ComicSearchResult Metadata, Uri? CoverUrl);

    // The BnF knows almost every French comic (dépôt légal); Bubble knows comics the others miss;
    // Bedetheque, often blocked by Cloudflare and costing SerpApi calls, comes last.
    private async Task<ProviderMatch?> FindFirstAsync(string isbn, CancellationToken cancellationToken)
    {
        var bnfResult = await _bnfCatalogueService.SearchByIsbnAsync(isbn, cancellationToken);
        if (bnfResult.Found)
        {
            _logger.LogInformation("Book found via the BnF catalogue for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapBookResultSync(bnfResult, isbn, string.Empty) with { Source = "BnF" }, await FindCoverAsync(isbn, cancellationToken));
        }

        var googleResult = await _googleBooksService.SearchByIsbnAsync(isbn, cancellationToken);
        if (googleResult.Found)
        {
            _logger.LogInformation("Book found via Google Books for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapBookResultSync(googleResult, isbn, string.Empty) with { Source = "Google Books" }, googleResult.CoverUrl);
        }

        var olResult = await _openLibraryService.SearchByIsbnAsync(isbn, cancellationToken);
        if (olResult.Found)
        {
            _logger.LogInformation("Book found via OpenLibrary for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapBookResultSync(olResult, isbn, string.Empty) with { Source = "Open Library" }, olResult.CoverUrl);
        }

        var bubbleResult = await _bubbleService.SearchByIsbnAsync(isbn, cancellationToken);
        if (bubbleResult.Found)
        {
            _logger.LogInformation("Book found via Bubble for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapStructuredResult(bubbleResult, isbn) with { Source = "Bubble" }, bubbleResult.CoverUrl);
        }

        var bedethequeResult = await _bedethequeService.SearchByIsbnAsync(isbn, cancellationToken);
        if (bedethequeResult.Found)
        {
            _logger.LogInformation("Book found via Bedetheque for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapStructuredResult(bedethequeResult, isbn) with { Source = "Bedetheque" }, bedethequeResult.CoverUrl);
        }

        _logger.LogWarning("No data found for ISBN {Isbn} in any provider", isbn);
        return null;
    }

    // The BnF catalogue has no cover: Google Books, then OpenLibrary, then Bubble may have one.
    private async Task<Uri?> FindCoverAsync(string isbn, CancellationToken cancellationToken)
    {
        var googleResult = await _googleBooksService.SearchByIsbnAsync(isbn, cancellationToken);
        if (googleResult is { Found: true, CoverUrl: not null })
        {
            return googleResult.CoverUrl;
        }

        var olResult = await _openLibraryService.SearchByIsbnAsync(isbn, cancellationToken);
        if (olResult is { Found: true, CoverUrl: not null })
        {
            return olResult.CoverUrl;
        }

        var bubbleResult = await _bubbleService.SearchByIsbnAsync(isbn, cancellationToken);
        return bubbleResult.Found ? bubbleResult.CoverUrl : null;
    }

    // The ImageUrl is set once the cover is uploaded.
    private ComicSearchResult MapStructuredResult(IStructuredBookResult result, string isbn)
    {
        _logger.LogInformation("Mapped result: {Serie} T{Volume} - {Title}",
            result.Serie, result.VolumeNumber, result.Title);

        return new ComicSearchResult(
            Title: result.Title,
            Serie: result.Serie,
            Isbn: isbn,
            VolumeNumber: result.VolumeNumber,
            ImageUrl: string.Empty,
            Authors: string.Join(", ", result.Authors),
            Publishers: string.Join(", ", result.Publishers),
            PublishDate: result.PublishDate,
            NumberOfPages: result.NumberOfPages,
            Found: true
        );
    }

    private ComicSearchResult MapBookResultSync(IBookSearchResult bookResult, string isbn, string imageUrl)
    {
        var (title, serie, parsedVolume) = ParseTitleInfo(bookResult);
        // A book whose volume is not known is stored as the first one.
        var volumeNumber = parsedVolume ?? 1;
        _logger.LogInformation("Found book: {Title} - {Serie} Vol.{Volume}", title, serie, volumeNumber);
        return new ComicSearchResult(
            Title: title,
            Serie: serie,
            Isbn: isbn,
            VolumeNumber: volumeNumber,
            ImageUrl: imageUrl,
            Authors: string.Join(", ", bookResult.Authors),
            Publishers: string.Join(", ", bookResult.Publishers),
            PublishDate: bookResult.PublishDate,
            NumberOfPages: bookResult.NumberOfPages,
            Found: true
        );
    }

    private async Task<string> UploadCoverStreamOrRemoteAsync(
        Stream? coverStream,
        string? coverFileName,
        Uri? remoteCoverUrl,
        string isbn,
        CancellationToken cancellationToken)
    {
        if (coverStream != null && coverFileName != null)
        {
            return await UploadLocalCoverAsync(coverStream, coverFileName, isbn, cancellationToken);
        }

        if (remoteCoverUrl != null)
        {
            return await UploadCoverToCloudinaryAsync(remoteCoverUrl, isbn, cancellationToken);
        }

        return string.Empty;
    }

    private async Task<string> UploadLocalCoverAsync(
        Stream coverStream,
        string coverFileName,
        string isbn,
        CancellationToken cancellationToken)
    {
        try
        {
            var publicId = string.IsNullOrEmpty(isbn)
                ? $"digital-{Guid.NewGuid():N}"
                : IsbnHelper.NormalizeIsbn(isbn);

            var uploadResult = await _cloudinaryService.UploadImageFromStreamAsync(
                coverStream,
                coverFileName,
                _cloudinarySettings.Folder,
                publicId,
                cancellationToken);

            if (uploadResult.Success && uploadResult.Url != null)
            {
                _logger.LogInformation("Local cover uploaded to Cloudinary: {Url}", uploadResult.Url);
                return uploadResult.Url.ToString();
            }

            _logger.LogWarning("Failed to upload local cover to Cloudinary: {Error}", uploadResult.Error);
        }
        catch (Exception ex) when (IsUnexpectedException(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Unexpected error uploading local cover for ISBN {Isbn}", isbn);
        }

        return string.Empty;
    }

    private async Task<string> UploadCoverToCloudinaryAsync(Uri coverUrl, string isbn, CancellationToken cancellationToken)
    {
        try
        {
            var cleanIsbn = IsbnHelper.NormalizeIsbn(isbn);

            var uploadResult = await _cloudinaryService.UploadImageFromUrlAsync(
                coverUrl,
                _cloudinarySettings.Folder,
                cleanIsbn,
                cancellationToken);

            if (uploadResult.Success && uploadResult.Url != null)
            {
                _logger.LogInformation("Cover uploaded to Cloudinary: {Url}", uploadResult.Url);
                return uploadResult.Url.ToString();
            }

            _logger.LogWarning("Failed to upload cover to Cloudinary: {Error}. Using original URL.", uploadResult.Error);
        }
        catch (Exception ex) when (IsUnexpectedException(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Unexpected error uploading cover to Cloudinary for {CoverUrl}. Using original URL.", coverUrl);
        }

        return coverUrl.ToString();
    }

    // Generated regex patterns for parsing volume and series from titles
    // "Soda, tome 1"
    [GeneratedRegex(@"^(.+?),\s*tome\s+(\d+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CommaTomePattern();

    // "Soda - tome 1"
    [GeneratedRegex(@"^(.+?)\s*-\s*tome\s+(\d+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DashTomePattern();

    // "Soda Tome 1" or "Fullmetal Alchemist Tome 23"
    [GeneratedRegex(@"^(.+?)\s+tome\s+(\d+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SpaceTomePattern();

    // "Soda, vol. 1" or "Soda, vol 1"
    [GeneratedRegex(@"^(.+?),\s*vol\.?\s*(\d+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CommaVolPattern();

    // "Soda - vol. 1"
    [GeneratedRegex(@"^(.+?)\s*-\s*vol\.?\s*(\d+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DashVolPattern();

    // "Soda vol. 1"
    [GeneratedRegex(@"^(.+?)\s+vol\.?\s*(\d+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SpaceVolPattern();

    // "Soda #1"
    [GeneratedRegex(@"^(.+?)\s*#(\d+)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex HashPattern();

    // "Les Schtroumpfs. 12, Le schtroumpfissime"
    [GeneratedRegex(@"^(?<serie>.+?)\.\s+(?<volume>\d+)\s*,\s*(?<title>.+)$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NumberedTitlePattern();

    // The volume is null when the title names none.
    private static (string Serie, int? VolumeNumber) ParseVolumeAndSerie(string fullTitle)
    {
        if (string.IsNullOrWhiteSpace(fullTitle))
        {
            return (string.Empty, null);
        }

        int? volumeNumber = null;
        var serie = string.Empty;

        // Try each pattern in order until one matches
        var patterns = new Func<Regex>[]
        {
            CommaTomePattern,
            DashTomePattern,
            SpaceTomePattern,
            CommaVolPattern,
            DashVolPattern,
            SpaceVolPattern,
            HashPattern
        };

        foreach (var patternFunc in patterns)
        {
            var match = patternFunc().Match(fullTitle);
            if (match.Success)
            {
                serie = match.Groups[1].Value.Trim();
                if (int.TryParse(match.Groups[2].Value, out var vol))
                {
                    volumeNumber = vol;
                }
                break;
            }
        }

        // If no series was extracted, use title as both
        if (string.IsNullOrEmpty(serie))
        {
            serie = fullTitle;
        }

        return (serie, volumeNumber);
    }

    // Distinguishes a genuine caller-requested cancellation (must propagate) from an internal
    // timeout or other failure surfaced as an exception (must degrade to a not-found result).
    private static bool IsUnexpectedException(Exception ex, CancellationToken cancellationToken) =>
        ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

    private static ComicSearchResult CreateNotFoundResult(string isbn) =>
        new(
            Title: string.Empty,
            Serie: string.Empty,
            Isbn: isbn,
            VolumeNumber: 1,
            ImageUrl: string.Empty,
            Authors: string.Empty,
            Publishers: string.Empty,
            PublishDate: null,
            NumberOfPages: null,
            Found: false
        );
}
