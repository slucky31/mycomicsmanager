using System.Globalization;
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
    private readonly ICloudinaryService _cloudinaryService;
    private readonly CloudinarySettings _cloudinarySettings;

    public ComicSearchService(
        IOpenLibraryService openLibraryService,
        IGoogleBooksService googleBooksService,
        ICloudinaryService cloudinaryService,
        IOptions<CloudinarySettings> cloudinarySettings)
    {
        _openLibraryService = openLibraryService;
        _googleBooksService = googleBooksService;
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


            // Try OpenLibrary first
            var result = await _openLibraryService.SearchByIsbnAsync(cleanIsbn, cancellationToken);

            if (result.Found)
            {
                Log.Information("Book found via OpenLibrary for ISBN {Isbn}", cleanIsbn);
                return await MapBookResultToComicSearchResultAsync(
                    result, cleanIsbn, cancellationToken);
            }

            // Fallback to Google Books
            Log.Information("OpenLibrary returned no result for ISBN {Isbn}, trying Google Books", cleanIsbn);
            var googleResult = await _googleBooksService.SearchByIsbnAsync(cleanIsbn, cancellationToken);

            if (googleResult.Found)
            {
                Log.Information("Book found via Google Books for ISBN {Isbn}", cleanIsbn);
                return await MapBookResultToComicSearchResultAsync(
                    googleResult, cleanIsbn, cancellationToken);
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
        var publishDate = ParsePublishDate(bookResult.PublishDate);

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

    private async Task<string> UploadCoverToCloudinaryAsync(Uri coverUrl, string isbn, CancellationToken cancellationToken)
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

    private static DateOnly? ParsePublishDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        // OpenLibrary returns dates in various formats:
        // "September 16, 1987", "1987", "Sep 1987", "1987-09-16", etc.
        var formats = new[]
        {
            "MMMM d, yyyy",      // "September 16, 1987"
            "MMMM dd, yyyy",     // "September 16, 1987"
            "MMM d, yyyy",       // "Sep 16, 1987"
            "MMM dd, yyyy",      // "Sep 16, 1987"
            "yyyy-MM-dd",        // "1987-09-16"
            "yyyy/MM/dd",        // "1987/09/16"
            "dd/MM/yyyy",        // "16/09/1987"
            "MM/dd/yyyy",        // "09/16/1987"
            "MMMM yyyy",         // "September 1987"
            "MMM yyyy",          // "Sep 1987"
            "yyyy",              // "1987"
        };

        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(dateString.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        // Try generic parsing as fallback
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        Log.Warning("Unable to parse publish date: {DateString}", dateString);
        return null;
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
