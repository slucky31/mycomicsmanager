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
            var (serie, title, volumeNumber) = ParseTitleAndSeries(result.Title);

            // Upload cover to Cloudinary if available
            var imageUrl = string.Empty;
            if (result.CoverUrl != null)
            {
                imageUrl = await UploadCoverToCloudinaryAsync(result.CoverUrl, isbn, cancellationToken);
            }

            Log.Information("Found book: {Title} - {Serie} Vol.{Volume}", title, serie, volumeNumber);

            return new ComicSearchResult(
                Title: title,
                Serie: serie,
                Isbn: isbn,
                VolumeNumber: volumeNumber,
                ImageUrl: imageUrl,
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

    private static (string Serie, string Title, int VolumeNumber) ParseTitleAndSeries(string fullTitle)
    {
        if (string.IsNullOrWhiteSpace(fullTitle))
        {
            return (string.Empty, string.Empty, 1);
        }

        var volumeNumber = 1;
        var title = fullTitle;
        var serie = string.Empty;

        // Common patterns for comic/manga titles:
        // "Series Name - Tome 1"
        // "Series Name - Tome 1 - Subtitle"
        // "Series Name, Vol. 2"
        // "Series Name #3"

        // Try to extract volume number
        var tomeMatch = System.Text.RegularExpressions.Regex.Match(
            fullTitle,
            @"[-–]\s*[Tt]ome\s*(\d+)",
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));

        if (tomeMatch.Success)
        {
            _ = int.TryParse(tomeMatch.Groups[1].Value, out volumeNumber);
            // Series is everything before "- Tome X"
            var tomeIndex = fullTitle.IndexOf(tomeMatch.Value, StringComparison.OrdinalIgnoreCase);
            if (tomeIndex > 0)
            {
                serie = fullTitle[..tomeIndex].Trim().TrimEnd('-', '–').Trim();
                // Title might include what comes after "Tome X"
                var afterTome = fullTitle[(tomeIndex + tomeMatch.Value.Length)..].Trim().TrimStart('-', '–').Trim();
                title = string.IsNullOrEmpty(afterTome) ? serie : afterTome;
            }
        }
        else
        {
            // Try "Vol. X" pattern
            var volMatch = System.Text.RegularExpressions.Regex.Match(
                fullTitle,
                @",?\s*[Vv]ol\.?\s*(\d+)",
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromSeconds(1));

            if (volMatch.Success)
            {
                _ = int.TryParse(volMatch.Groups[1].Value, out volumeNumber);
                var volIndex = fullTitle.IndexOf(volMatch.Value, StringComparison.OrdinalIgnoreCase);
                if (volIndex > 0)
                {
                    serie = fullTitle[..volIndex].Trim().TrimEnd(',').Trim();
                    title = serie;
                }
            }
            else
            {
                // Try "#X" pattern
                var hashMatch = System.Text.RegularExpressions.Regex.Match(
                    fullTitle,
                    @"\s*#(\d+)",
                    System.Text.RegularExpressions.RegexOptions.None,
                    TimeSpan.FromSeconds(1));

                if (hashMatch.Success)
                {
                    _ = int.TryParse(hashMatch.Groups[1].Value, out volumeNumber);
                    var hashIndex = fullTitle.IndexOf(hashMatch.Value, StringComparison.OrdinalIgnoreCase);
                    if (hashIndex > 0)
                    {
                        serie = fullTitle[..hashIndex].Trim();
                        title = serie;
                    }
                }
            }
        }

        // If no series was extracted, use title as both
        if (string.IsNullOrEmpty(serie))
        {
            serie = title;
        }

        if (volumeNumber == 0)
        {
            volumeNumber = 1;
        }

        return (serie, title, volumeNumber);
    }

    private static ComicSearchResult CreateNotFoundResult(string isbn) =>
        new(
            Title: string.Empty,
            Serie: string.Empty,
            Isbn: isbn,
            VolumeNumber: 1,
            ImageUrl: string.Empty,
            Found: false
        );
}
