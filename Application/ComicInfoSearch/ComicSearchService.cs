using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

public class ComicSearchService : IComicSearchService
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<ComicSearchService>();

    private readonly IOpenLibraryService _openLibraryService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly CloudinarySettings _cloudinarySettings;

    public ComicSearchService(
        IOpenLibraryService openLibraryService,
        ICloudinaryService cloudinaryService,
        IOptions<CloudinarySettings> cloudinarySettings)
    {
        _openLibraryService = openLibraryService;
        _cloudinaryService = cloudinaryService;
        _cloudinarySettings = cloudinarySettings.Value;
    }

    public async Task<ComicSearchResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _openLibraryService.SearchByIsbnAsync(isbn, cancellationToken);

            if (!result.Found)
            {
                Log.Warning("No data found for ISBN {Isbn}", isbn);
                return CreateNotFoundResult(isbn);
            }

            // Extract series and volume from title if possible
            // OpenLibrary often has titles like "Series Name - Tome 1" or "Series Name, Vol. 2"
            var (serie, volumeNumber) = ParseVolumeAndSerie(result.Title);

            var title = string.IsNullOrEmpty(result.Subtitle) ? result.Title : result.Subtitle;

            // Upload cover to Cloudinary if available
            var imageUrl = string.Empty;
            if (result.CoverUrl != null)
            {
                imageUrl = await UploadCoverToCloudinaryAsync(result.CoverUrl, isbn, cancellationToken);
            }

            // Combine authors and publishers as comma-separated strings
            var authors = string.Join(", ", result.Authors);
            var publishers = string.Join(", ", result.Publishers);

            // Parse publish date from OpenLibrary format
            var publishDate = ParsePublishDate(result.PublishDate);

            Log.Information("Found book: {Title} - {Serie} Vol.{Volume}", result.Subtitle, serie, volumeNumber);

            return new ComicSearchResult(
                Title: title,
                Serie: serie,
                Isbn: isbn,
                VolumeNumber: volumeNumber,
                ImageUrl: imageUrl,
                Authors: authors,
                Publishers: publishers,
                PublishDate: publishDate,
                NumberOfPages: result.NumberOfPages,
                Found: true
            );
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP error searching for ISBN {Isbn}", isbn);
            return CreateNotFoundResult(isbn);
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Invalid operation searching for ISBN {Isbn}", isbn);
            return CreateNotFoundResult(isbn);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Error(ex, "Timeout searching for ISBN {Isbn}", isbn);
            return CreateNotFoundResult(isbn);
        }
    }

    private async Task<string> UploadCoverToCloudinaryAsync(Uri coverUrl, string isbn, CancellationToken cancellationToken)
    {
        var cleanIsbn = isbn.Replace("-", "", StringComparison.Ordinal)
                           .Replace(" ", "", StringComparison.Ordinal);

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

    private static (string Serie, int VolumeNumber) ParseVolumeAndSerie(string fullTitle)
    {
        if (string.IsNullOrWhiteSpace(fullTitle))
        {
            return (string.Empty, 1);
        }

        var volumeNumber = 1;
        var serie = string.Empty;

        // Common patterns for comic/manga titles:
        // "Soda, tome 1"
        // "Series Name - Tome 2"
        // "Series Name, Vol. 3"
        // "Series Name Vol. 4"
        // "Series Name #5"
        var patterns = new[]
        {
            @"^(.+?),\s*tome\s+(\d+)",           // "Soda, tome 1"
            @"^(.+?)\s*-\s*tome\s+(\d+)",        // "Soda - tome 1"
            @"^(.+?),\s*vol\.?\s*(\d+)",         // "Soda, vol. 1" or "Soda, vol 1"
            @"^(.+?)\s*-\s*vol\.?\s*(\d+)",      // "Soda - vol. 1"
            @"^(.+?)\s+vol\.?\s*(\d+)",          // "Soda vol. 1"
            @"^(.+?)\s*#(\d+)",                  // "Soda #1"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(fullTitle, pattern, RegexOptions.IgnoreCase);
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

        // If only a year is provided as a number
        if (int.TryParse(dateString.Trim(), out var year) && year >= 1000 && year <= 9999)
        {
            return new DateOnly(year, 1, 1);
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
