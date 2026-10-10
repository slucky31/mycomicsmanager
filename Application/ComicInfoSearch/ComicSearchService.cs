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
    private readonly ICloudinaryService _cloudinaryService;
    private readonly CloudinarySettings _cloudinarySettings;
    private readonly ILogger<ComicSearchService> _logger;

    public ComicSearchService(
        IBnfCatalogueService bnfCatalogueService,
        IOpenLibraryService openLibraryService,
        IGoogleBooksService googleBooksService,
        IBedethequeService bedethequeService,
        ICloudinaryService cloudinaryService,
        IOptions<CloudinarySettings> cloudinarySettings,
        ILogger<ComicSearchService> logger)
    {
        _logger = logger;
        _bnfCatalogueService = bnfCatalogueService;
        _openLibraryService = openLibraryService;
        _googleBooksService = googleBooksService;
        _bedethequeService = bedethequeService;
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

    public (string Title, string Serie, int VolumeNumber) ParseTitleInfo(string rawTitle, string? subtitle)
    {
        var (serie, volumeNumber) = ParseVolumeAndSerie(rawTitle);
        var title = string.IsNullOrEmpty(subtitle) ? serie : subtitle;
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

    // The BnF knows almost every French comic (dépôt légal); Bedetheque, often blocked by Cloudflare, comes last.
    private async Task<ProviderMatch?> FindFirstAsync(string isbn, CancellationToken cancellationToken)
    {
        var bnfResult = await _bnfCatalogueService.SearchByIsbnAsync(isbn, cancellationToken);
        if (bnfResult.Found)
        {
            _logger.LogInformation("Book found via the BnF catalogue for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapBookResultSync(bnfResult, isbn, string.Empty), await FindCoverAsync(isbn, cancellationToken));
        }

        var googleResult = await _googleBooksService.SearchByIsbnAsync(isbn, cancellationToken);
        if (googleResult.Found)
        {
            _logger.LogInformation("Book found via Google Books for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapBookResultSync(googleResult, isbn, string.Empty), googleResult.CoverUrl);
        }

        var olResult = await _openLibraryService.SearchByIsbnAsync(isbn, cancellationToken);
        if (olResult.Found)
        {
            _logger.LogInformation("Book found via OpenLibrary for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapBookResultSync(olResult, isbn, string.Empty), olResult.CoverUrl);
        }

        var bedethequeResult = await _bedethequeService.SearchByIsbnAsync(isbn, cancellationToken);
        if (bedethequeResult.Found)
        {
            _logger.LogInformation("Book found via Bedetheque for ISBN {Isbn}", isbn);
            return new ProviderMatch(MapBedethequeResultSync(bedethequeResult, isbn, string.Empty), bedethequeResult.CoverUrl);
        }

        _logger.LogWarning("No data found for ISBN {Isbn} in any provider", isbn);
        return null;
    }

    // The BnF catalogue has no cover: Google Books, then OpenLibrary, may have one.
    private async Task<Uri?> FindCoverAsync(string isbn, CancellationToken cancellationToken)
    {
        var googleResult = await _googleBooksService.SearchByIsbnAsync(isbn, cancellationToken);
        if (googleResult is { Found: true, CoverUrl: not null })
        {
            return googleResult.CoverUrl;
        }

        var olResult = await _openLibraryService.SearchByIsbnAsync(isbn, cancellationToken);
        return olResult.Found ? olResult.CoverUrl : null;
    }

    private ComicSearchResult MapBedethequeResultSync(
        BedethequeBookResult bedethequeResult, string isbn, string imageUrl)
    {
        _logger.LogInformation("Mapped Bedetheque result: {Serie} T{Volume} - {Title}",
            bedethequeResult.Serie, bedethequeResult.VolumeNumber, bedethequeResult.Title);

        return new ComicSearchResult(
            Title: bedethequeResult.Title,
            Serie: bedethequeResult.Serie,
            Isbn: isbn,
            VolumeNumber: bedethequeResult.VolumeNumber,
            ImageUrl: imageUrl,
            Authors: string.Join(", ", bedethequeResult.Authors),
            Publishers: string.Join(", ", bedethequeResult.Publishers),
            PublishDate: bedethequeResult.PublishDate,
            NumberOfPages: bedethequeResult.NumberOfPages,
            Found: true
        );
    }

    private ComicSearchResult MapBookResultSync(IBookSearchResult bookResult, string isbn, string imageUrl)
    {
        var (title, serie, volumeNumber) = ParseTitleInfo(bookResult.Title, bookResult.Subtitle);
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

    private static (string Serie, int VolumeNumber) ParseVolumeAndSerie(string fullTitle)
    {
        if (string.IsNullOrWhiteSpace(fullTitle))
        {
            return (string.Empty, 1);
        }

        var volumeNumber = 1;
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
