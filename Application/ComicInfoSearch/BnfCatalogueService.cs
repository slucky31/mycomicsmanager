using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

/// <summary>
/// Searches the BnF catalogue through its SRU API and reads the Dublin Core record, e.g.
/// title "Tangente / Céline Wagner", creator "Wagner, Céline (1975-....). Auteur du texte",
/// publisher "des Ronds dans l'O (Vincennes)", format "1 vol. (82 p.) : ill.".
/// </summary>
public partial class BnfCatalogueService : IBnfCatalogueService
{
    private static readonly XNamespace s_dc = "http://purl.org/dc/elements/1.1/";

    private readonly HttpClient _httpClient;
    private readonly BnfCatalogueSettings _settings;
    private readonly ILogger<BnfCatalogueService> _logger;

    public BnfCatalogueService(HttpClient httpClient, IOptions<BnfCatalogueSettings> settings, ILogger<BnfCatalogueService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    // "Wagner, Céline (1975-....). Auteur du texte": the family name, the given names, then dates and role.
    [GeneratedRegex(@"^(?<family>[^,(]+),\s*(?<given>[^(]+?)\s*(?:\(|\.\s|$)")]
    private static partial Regex PersonPattern();

    [GeneratedRegex(@"(?<pages>\d+)\s*p\.")]
    private static partial Regex PagesPattern();

    [GeneratedRegex(@"\d{4}")]
    private static partial Regex YearPattern();

    public async Task<BnfBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var cleanIsbn = IsbnHelper.NormalizeIsbn(isbn);

        try
        {
            var query = Uri.EscapeDataString($"bib.fuzzyISBN all \"{cleanIsbn}\"");
            var url = new Uri(_settings.BaseUrl,
                $"/api/SRU?version=1.2&operation=searchRetrieve&query={query}&recordSchema=dublincore&maximumRecords=1");

            _logger.LogInformation("Searching the BnF catalogue for ISBN: {Isbn}", cleanIsbn);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("BnF catalogue returned {StatusCode} for ISBN: {Isbn}", response.StatusCode, cleanIsbn);
                return CreateFailedResult();
            }

            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            var record = XDocument.Parse(xml).Descendants(s_dc + "title").FirstOrDefault()?.Parent;
            if (record is null)
            {
                _logger.LogWarning("No record found in the BnF catalogue for ISBN: {Isbn}", cleanIsbn);
                return CreateNotFoundResult();
            }

            return ParseRecord(record);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching the BnF catalogue for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "XML parsing error for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout searching the BnF catalogue for ISBN: {Isbn}", cleanIsbn);
            return CreateFailedResult();
        }
    }

    private static BnfBookResult ParseRecord(XElement record)
    {
        var (title, subtitle) = ParseTitle(Values(record, "title").FirstOrDefault() ?? string.Empty);

        return new BnfBookResult(
            Title: title,
            Subtitle: subtitle,
            Authors: [.. Values(record, "creator").Concat(Values(record, "contributor")).Select(ToPersonName).Distinct(StringComparer.Ordinal)],
            Publishers: [.. Values(record, "publisher").Select(StripTrailingPlace).Distinct(StringComparer.Ordinal)],
            PublishDate: ParseYear(Values(record, "date").FirstOrDefault()),
            NumberOfPages: ParsePages(Values(record, "format").FirstOrDefault()),
            CoverUrl: null,
            Found: !string.IsNullOrWhiteSpace(title));
    }

    private static IEnumerable<string> Values(XElement record, string name) =>
        record.Elements(s_dc + name).Select(e => e.Value.Trim()).Where(v => v.Length > 0);

    // "Tangente / Céline Wagner" → "Tangente"; "Titre : sous-titre / Auteur" → ("Titre", "sous-titre").
    internal static (string Title, string? Subtitle) ParseTitle(string value)
    {
        var withoutResponsibility = value.Split(" / ", 2)[0].Trim();
        var parts = withoutResponsibility.Split(" : ", 2);
        return parts.Length == 2 ? (parts[0].Trim(), parts[1].Trim()) : (withoutResponsibility, null);
    }

    // "Wagner, Céline (1975-....). Auteur du texte" → "Céline Wagner"; a collectivity is kept as written.
    internal static string ToPersonName(string value)
    {
        var match = PersonPattern().Match(value);
        if (match.Success)
        {
            return $"{match.Groups["given"].Value.Trim()} {match.Groups["family"].Value.Trim()}";
        }

        var end = value.IndexOfAny(['(', '.']);
        return (end > 0 ? value[..end] : value).Trim();
    }

    // "des Ronds dans l'O (Vincennes)" → "des Ronds dans l'O".
    internal static string StripTrailingPlace(string value)
    {
        var open = value.LastIndexOf(" (", StringComparison.Ordinal);
        return open > 0 && value.EndsWith(')') ? value[..open].Trim() : value;
    }

    // Convention of the application: a year alone is stored as January 1st of that year.
    private static DateOnly? ParseYear(string? value)
    {
        var match = value is null ? Match.Empty : YearPattern().Match(value);
        return match.Success ? new DateOnly(int.Parse(match.Value, CultureInfo.InvariantCulture), 1, 1) : null;
    }

    private static int? ParsePages(string? value)
    {
        var match = value is null ? Match.Empty : PagesPattern().Match(value);
        return match.Success && int.TryParse(match.Groups["pages"].Value, CultureInfo.InvariantCulture, out var pages) ? pages : null;
    }

    private static BnfBookResult CreateFailedResult() => CreateNotFoundResult() with { Failed = true };

    private static BnfBookResult CreateNotFoundResult() =>
        new(string.Empty, null, [], [], null, null, null, Found: false);
}
