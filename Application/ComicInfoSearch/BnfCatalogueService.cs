using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Application.Helpers;
using Application.Interfaces;
using Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.ComicInfoSearch;

/// <summary>
/// Searches the BnF catalogue through its SRU API and reads the UNIMARC record: title (200),
/// publisher and date (210/214), pages (215), series and volume (461, else 225) and authors (70x/71x).
/// </summary>
public partial class BnfCatalogueService : IBnfCatalogueService
{
    private static readonly XNamespace s_marc = "info:lc/xmlns/marcxchange-v2";

    // Authors in the order of their responsibility: main, joint, then secondary.
    private static readonly string[] s_personTags = ["700", "701", "702"];
    private static readonly string[] s_collectivityTags = ["710", "711", "712"];

    private readonly HttpClient _httpClient;
    private readonly BnfCatalogueSettings _settings;
    private readonly IFeatureToggles _featureToggles;
    private readonly ILogger<BnfCatalogueService> _logger;

    public BnfCatalogueService(HttpClient httpClient, IOptions<BnfCatalogueSettings> settings, IFeatureToggles featureToggles, ILogger<BnfCatalogueService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _featureToggles = featureToggles;
        _logger = logger;
    }

    [GeneratedRegex(@"(?<pages>\d+)\s*p\.")]
    private static partial Regex PagesPattern();

    [GeneratedRegex(@"\d{4}")]
    private static partial Regex YearPattern();

    public bool IsEnabled => _featureToggles.IsEnabled(FeatureToggle.Bnf);

    public async Task<BnfBookResult> SearchByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var cleanIsbn = IsbnHelper.NormalizeIsbn(isbn);

        if (!IsEnabled)
        {
            return CreateNotFoundResult();
        }

        try
        {
            var query = Uri.EscapeDataString($"bib.fuzzyISBN all \"{cleanIsbn}\"");
            var url = new Uri(_settings.BaseUrl,
                $"/api/SRU?version=1.2&operation=searchRetrieve&query={query}&recordSchema=unimarcXchange&maximumRecords=1");

            _logger.LogInformation("Searching the BnF catalogue for ISBN: {Isbn}", cleanIsbn);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("BnF catalogue returned {StatusCode} for ISBN: {Isbn}", response.StatusCode, cleanIsbn);
                return CreateFailedResult();
            }

            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            var record = XDocument.Parse(xml).Descendants(s_marc + "record").FirstOrDefault();
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
        var titleField = Fields(record, "200").FirstOrDefault();
        var title = FirstSubfield(titleField, "a") ?? string.Empty;
        var (serie, volumeNumber) = ParseSerie(record);

        // An old record names the volume in the title: "Les Schtroumpfs" $h "12" $i "Le schtroumpfissime".
        var partName = FirstSubfield(titleField, "i");
        if (partName is not null)
        {
            serie ??= title;
            volumeNumber ??= SeriesHelper.ParseVolumeNumber(FirstSubfield(titleField, "h"));
            title = partName;
        }

        var publication = Fields(record, "210").Concat(Fields(record, "214")).ToList();

        return new BnfBookResult(
            Title: title,
            Subtitle: FirstSubfield(titleField, "e"),
            Authors: [.. ParseAuthors(record)],
            Publishers: [.. publication.SelectMany(field => Subfields(field, "c")).Distinct(StringComparer.Ordinal)],
            PublishDate: ParseYear(publication.SelectMany(field => Subfields(field, "d")).FirstOrDefault()),
            NumberOfPages: ParsePages(Fields(record, "215").Select(field => FirstSubfield(field, "a")).FirstOrDefault()),
            CoverUrl: null,
            Found: !string.IsNullOrWhiteSpace(title),
            Serie: serie,
            VolumeNumber: volumeNumber);
    }

    // The series the book belongs to (461), else the series statement (225), with its volume ("[6]").
    private static (string? Serie, int? VolumeNumber) ParseSerie(XElement record)
    {
        var link = Fields(record, "461").FirstOrDefault();
        var field = link ?? Fields(record, "225").FirstOrDefault();
        var serie = FirstSubfield(field, link is null ? "a" : "t");
        return serie is null ? (null, null) : (serie, SeriesHelper.ParseVolumeNumber(FirstSubfield(field, "v")));
    }

    // "Tardi" $b "Jacques" → "Jacques Tardi"; a collectivity is kept as written.
    private static IEnumerable<string> ParseAuthors(XElement record)
    {
        var persons = s_personTags.SelectMany(tag => Fields(record, tag)).Select(field =>
        {
            var family = FirstSubfield(field, "a");
            var given = FirstSubfield(field, "b");
            return given is null ? family : $"{given} {family}";
        });
        var collectivities = s_collectivityTags.SelectMany(tag => Fields(record, tag)).Select(field => FirstSubfield(field, "a"));

        return persons.Concat(collectivities).OfType<string>().Distinct(StringComparer.Ordinal);
    }

    private static IEnumerable<XElement> Fields(XElement record, string tag) =>
        record.Elements(s_marc + "datafield").Where(field => (string?)field.Attribute("tag") == tag);

    // The BnF marks the articles to skip when sorting with control characters ("\u0098Le \u009CNoyé").
    private static IEnumerable<string> Subfields(XElement? field, string code) =>
        field is null
            ? []
            : field.Elements(s_marc + "subfield")
                .Where(subfield => (string?)subfield.Attribute("code") == code)
                .Select(subfield => new string([.. subfield.Value.Where(c => !char.IsControl(c))]).Trim())
                .Where(value => value.Length > 0);

    private static string? FirstSubfield(XElement? field, string code) => Subfields(field, code).FirstOrDefault();

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
