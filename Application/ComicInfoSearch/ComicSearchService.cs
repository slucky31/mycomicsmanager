using System.Text.RegularExpressions;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

public partial class ComicSearchService : IComicSearchService
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ComicSearchService>();

    private readonly IOpenLibraryService _openLibraryService;
    private readonly IGoogleBooksService _googleBooksService;
    private readonly IBedethequeService _bedethequeService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly CloudinarySettings _cloudinarySettings;

    public ComicSearchService(
        IOpenLibraryService openLibraryService,
        IGoogleBooksService googleBooksService,
        IBedethequeService bedethequeService,
        ICloudinaryService cloudinaryService,
        IOptions<CloudinarySettings> cloudinarySettings)
    {
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
            // Try Bedetheque first
            var bedethequeResult = await _bedethequeService.SearchByIsbnAsync(cleanIsbn, cancellationToken);

            if (bedethequeResult.Found)
            {
                Log.Information("Book found via Bedetheque for ISBN {Isbn}", cleanIsbn);
                return await MapBedethequeResultAsync(bedethequeResult, cleanIsbn, cancellationToken);
            }

            // Fallback to Google Books
            Log.Information("Bedetheque returned no result for ISBN {Isbn}, trying Google Books", cleanIsbn);
            var googleResult = await _googleBooksService.SearchByIsbnAsync(cleanIsbn, cancellationToken);

            if (googleResult.Found)
            {
                Log.Information("Book found via Google Books for ISBN {Isbn}", cleanIsbn);
                return await MapBookResultToComicSearchResultAsync(
                    googleResult, cleanIsbn, cancellationToken);
            }

            // Fallback to OpenLibrary
            Log.Information("Google Books returned no result for ISBN {Isbn}, trying OpenLibrary", cleanIsbn);
            var olResult = await _openLibraryService.SearchByIsbnAsync(cleanIsbn, cancellationToken);

            if (olResult.Found)
            {
                Log.Information("Book found via OpenLibrary for ISBN {Isbn}", cleanIsbn);
                return await MapBookResultToComicSearchResultAsync(olResult, cleanIsbn, cancellationToken);
            }

            Log.Warning("No data found for ISBN {Isbn} in any provider", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error searching for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Invalid operation searching for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error(ex, "Timeout searching for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            Log.Warning(ex, "Search cancelled for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
    }

    public (string Title, string Serie, int VolumeNumber) ParseTitleInfo(string rawTitle, string? subtitle)
    {
        var (serie, volumeNumber) = ParseVolumeAndSerie(rawTitle);
        var title = string.IsNullOrEmpty(subtitle) ? serie : subtitle;
        return (title, serie, volumeNumber);
    }

    private async Task<ComicSearchResult> MapBedethequeResultAsync(
        BedethequeBookResult bedethequeResult,
        string isbn,
        CancellationToken cancellationToken)
    {
        var imageUrl = string.Empty;
        if (bedethequeResult.CoverUrl != null)
        {
            imageUrl = await UploadCoverToCloudinaryAsync(bedethequeResult.CoverUrl, isbn, cancellationToken);
        }

        var authors = string.Join(", ", bedethequeResult.Authors);
        var publishers = string.Join(", ", bedethequeResult.Publishers);

        Log.Information("Mapped Bedetheque result: {Serie} T{Volume} - {Title}", bedethequeResult.Serie, bedethequeResult.VolumeNumber, bedethequeResult.Title);

        return new ComicSearchResult(
            Title: bedethequeResult.Title,
            Serie: bedethequeResult.Serie,
            Isbn: isbn,
            VolumeNumber: bedethequeResult.VolumeNumber,
            ImageUrl: imageUrl,
            Authors: authors,
            Publishers: publishers,
            PublishDate: bedethequeResult.PublishDate,
            NumberOfPages: bedethequeResult.NumberOfPages,
            Found: true
        );
    }

    private async Task<ComicSearchResult> MapBookResultToComicSearchResultAsync(
        IBookSearchResult bookResult,
        string isbn,
        CancellationToken cancellationToken)
    {
        var (title, serie, volumeNumber) = ParseTitleInfo(bookResult.Title, bookResult.Subtitle);

        // Upload cover to Cloudinary if available
        var imageUrl = string.Empty;
        if (bookResult.CoverUrl != null)
        {
            imageUrl = await UploadCoverToCloudinaryAsync(bookResult.CoverUrl, isbn, cancellationToken);
        }

        var authors = string.Join(", ", bookResult.Authors);
        var publishers = string.Join(", ", bookResult.Publishers);
        var publishDate = bookResult.PublishDate;

        Log.Information("Found book: {Title} - {Serie} Vol.{Volume}", title, serie, volumeNumber);

        return new ComicSearchResult(
            Title: title,
            Serie: serie,
            Isbn: isbn,
            VolumeNumber: volumeNumber,
            ImageUrl: imageUrl,
            Authors: authors,
            Publishers: publishers,
            PublishDate: publishDate,
            NumberOfPages: bookResult.NumberOfPages,
            Found: true
        );
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

        try
        {
            if (!string.IsNullOrEmpty(cleanIsbn))
            {
                // Try Bedetheque first
                var bedethequeResult = await _bedethequeService.SearchByIsbnAsync(cleanIsbn, cancellationToken);
                if (bedethequeResult.Found)
                {
                    Log.Information("Book found via Bedetheque for ISBN {Isbn}", cleanIsbn);
                    var imageUrl = await UploadCoverStreamOrRemoteAsync(
                        coverStream, coverFileName, bedethequeResult.CoverUrl, cleanIsbn, cancellationToken);
                    return MapBedethequeResultSync(bedethequeResult, cleanIsbn, imageUrl);
                }

                // Fallback to Google Books
                Log.Information("Bedetheque returned no result for ISBN {Isbn}, trying Google Books", cleanIsbn);
                var googleResult = await _googleBooksService.SearchByIsbnAsync(cleanIsbn, cancellationToken);
                if (googleResult.Found)
                {
                    Log.Information("Book found via Google Books for ISBN {Isbn}", cleanIsbn);
                    var imageUrl = await UploadCoverStreamOrRemoteAsync(
                        coverStream, coverFileName, googleResult.CoverUrl, cleanIsbn, cancellationToken);
                    return MapBookResultSync(googleResult, cleanIsbn, imageUrl);
                }

                // Fallback to OpenLibrary
                Log.Information("Google Books returned no result for ISBN {Isbn}, trying OpenLibrary", cleanIsbn);
                var olResult = await _openLibraryService.SearchByIsbnAsync(cleanIsbn, cancellationToken);
                if (olResult.Found)
                {
                    Log.Information("Book found via OpenLibrary for ISBN {Isbn}", cleanIsbn);
                    var imageUrl = await UploadCoverStreamOrRemoteAsync(
                        coverStream, coverFileName, olResult.CoverUrl, cleanIsbn, cancellationToken);
                    return MapBookResultSync(olResult, cleanIsbn, imageUrl);
                }

                Log.Warning("No data found for ISBN {Isbn} in any provider", cleanIsbn);
            }

            // No metadata found – upload local cover only (guid-based publicId when no ISBN)
            var coverImageUrl = coverStream != null && coverFileName != null
                ? await UploadLocalCoverAsync(coverStream, coverFileName, cleanIsbn, cancellationToken)
                : string.Empty;

            return CreateNotFoundResult(cleanIsbn) with { ImageUrl = coverImageUrl };
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error searching for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Invalid operation searching for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error(ex, "Timeout searching for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            Log.Warning(ex, "Search cancelled for ISBN {Isbn}", cleanIsbn);
            return CreateNotFoundResult(cleanIsbn);
        }
    }

    private static ComicSearchResult MapBedethequeResultSync(
        BedethequeBookResult bedethequeResult, string isbn, string imageUrl) =>
        new(
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

    private ComicSearchResult MapBookResultSync(IBookSearchResult bookResult, string isbn, string imageUrl)
    {
        var (title, serie, volumeNumber) = ParseTitleInfo(bookResult.Title, bookResult.Subtitle);
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
                Log.Information("Local cover uploaded to Cloudinary: {Url}", uploadResult.Url);
                return uploadResult.Url.ToString();
            }

            Log.Warning("Failed to upload local cover to Cloudinary: {Error}", uploadResult.Error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warning(ex, "Unexpected error uploading local cover for ISBN {Isbn}", isbn);
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
                Log.Information("Cover uploaded to Cloudinary: {Url}", uploadResult.Url);
                return uploadResult.Url.ToString();
            }

            Log.Warning("Failed to upload cover to Cloudinary: {Error}. Using original URL.", uploadResult.Error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warning(ex, "Unexpected error uploading cover to Cloudinary for {CoverUrl}. Using original URL.", coverUrl);
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
