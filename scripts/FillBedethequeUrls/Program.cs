/*
 * FillBedethequeUrls — one-shot tool
 * ------------------------------------
 * Usage:
 *   dotnet run --project scripts/FillBedethequeUrls -- \
 *       --connection "Host=...;Database=...;Username=...;Password=..." \
 *       --serper-key "YOUR_KEY" \
 *       [--delay 500]
 *
 * The ConnectionStrings__Default and SerperApiKey env vars can be used instead of
 * --connection and --serper-key respectively.
 *
 * The tool will:
 *   1. Query all distinct ISBNs from Books that are not yet in IsbnBedethequeUrls
 *   2. For each ISBN, try three search formats in order (plain → hyphenated → short)
 *      via the Serper API (google.serper.dev):
 *        plain      : 9782205071153
 *        hyphenated : 978-2-205-07115-3  (ISBN range rules for 978-2 / 979-10)
 *        short      : 2-205-07115         (strip EAN prefix + check digit)
 *      For each format:
 *        - If exactly one /BD- link is returned → use it directly
 *        - If exactly one /serie- (non /serie-bd) link is returned → parse that
 *          page to find the album whose ISBN matches, then use its /BD- URL
 *   3. Store found URLs in IsbnBedethequeUrls via the cache repository
 */

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application;
using Application.Helpers;
using Application.Interfaces;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;

// ─── Arg parsing ──────────────────────────────────────────────────────────────

string? connectionString = null;
string? serperKey        = null;
int     delayMs          = 500;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--connection": connectionString = args[++i]; break;
        case "--serper-key": serperKey        = args[++i]; break;
        case "--delay":      delayMs          = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture); break;
    }
}

// ─── Validate ─────────────────────────────────────────────────────────────────

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

connectionString ??= config.GetConnectionString("Default");
serperKey        ??= config["SerperApiKey"];

if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("ERROR: --connection or ConnectionStrings__Default env var is required.");
    return 1;
}

if (string.IsNullOrEmpty(serperKey))
{
    Console.Error.WriteLine("ERROR: --serper-key or SerperApiKey env var is required.");
    return 1;
}

// ─── DI setup ─────────────────────────────────────────────────────────────────

var services = new ServiceCollection();
services.AddApplication();
services.AddInfrastructure(connectionString, rootPath: Path.GetTempPath(), config);

var provider = services.BuildServiceProvider();

// ─── Step 1 : Query ISBNs without cache ───────────────────────────────────────

List<string> isbns;
using (var scope = provider.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    isbns = await context.Books
        .Where(b => !string.IsNullOrEmpty(b.ISBN))
        .Where(b => !context.IsbnBedethequeUrls.Any(u => u.ISBN == b.ISBN))
        .Select(b => b.ISBN)
        .Distinct()
        .OrderBy(isbn => isbn)
        .ToListAsync();
}

Console.WriteLine($"{isbns.Count} ISBNs to process.");

if (isbns.Count == 0)
{
    Console.WriteLine("Nothing to do.");
    return 0;
}

// ─── Step 2 : Fetch Bedetheque URL for each ISBN ──────────────────────────────

const string BdUrlPrefix     = "https://www.bedetheque.com/BD";
const string SerieUrlPrefix  = "https://www.bedetheque.com/serie-";
const string SerieBdUrlPrefix = "https://www.bedetheque.com/serie-bd";
const string SerperEndpoint  = "https://google.serper.dev/search";

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("X-API-KEY", serperKey);
httpClient.Timeout = TimeSpan.FromSeconds(30);

using var bedeClient = new HttpClient();
bedeClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MyComicsManager/1.0)");
bedeClient.Timeout = TimeSpan.FromSeconds(30);

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

int found = 0, notFound = 0, failed = 0;
var ct = CancellationToken.None;

for (int i = 0; i < isbns.Count; i++)
{
    var isbn   = IsbnHelper.NormalizeIsbn(isbns[i]);
    var prefix = $"[{i + 1,4}/{isbns.Count}] {isbn}";

    try
    {
        string?[] formats = [isbn, IsbnHelper.ToHyphenatedIsbn(isbn), IsbnHelper.ToShortIsbn(isbn)];
        string? bdUrl = null;
        bool requestFailed = false;

        foreach (var searchIsbn in formats)
        {
            if (string.IsNullOrEmpty(searchIsbn)) continue;
            (bdUrl, requestFailed) = await TrySerperFormatAsync(searchIsbn, isbn);
            if (bdUrl is not null || requestFailed) break;
        }

        if (requestFailed)
        {
            failed++;
        }
        else if (bdUrl is not null)
        {
            using var scope = provider.CreateScope();
            var cacheRepo = scope.ServiceProvider.GetRequiredService<IIsbnBedethequeCacheRepository>();
            await cacheRepo.SaveAsync(isbn, bdUrl, ct);
            Console.WriteLine($"{prefix} → {bdUrl}");
            found++;
        }
        else
        {
            Console.WriteLine($"{prefix} → not found");
            notFound++;
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{prefix} → EXCEPTION: {ex.Message}");
        failed++;
    }

    if (i < isbns.Count - 1)
        await Task.Delay(delayMs, ct);
}

// ─── Summary ──────────────────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine($"Done — found: {found}, not found: {notFound}, failed: {failed}");
return failed > 0 ? 2 : 0;

// ─── Local functions ──────────────────────────────────────────────────────────

async Task<(string? url, bool failed)> TrySerperFormatAsync(string searchIsbn, string normalizedIsbn)
{
    var body = JsonSerializer.Serialize(new { q = $"{searchIsbn} site:bedetheque.com", gl = "fr", hl = "fr", num = 10 });
    using var request = new HttpRequestMessage(HttpMethod.Post, SerperEndpoint)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };

    var response = await httpClient.SendAsync(request, ct);
    if (!response.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"  ERROR HTTP {(int)response.StatusCode} for {searchIsbn}");
        return (null, true);
    }

    var serperResult = await response.Content.ReadFromJsonAsync<SerperResponse>(jsonOptions, ct);
    var links = serperResult?.Organic?.Select(r => r.Link).OfType<string>().ToList() ?? [];

    var bdLinks = links
        .Where(l => l.StartsWith(BdUrlPrefix, StringComparison.OrdinalIgnoreCase))
        .ToList();
    if (bdLinks.Count == 1) return (bdLinks[0], false);

    var serieLinks = links
        .Where(l => l.StartsWith(SerieUrlPrefix, StringComparison.OrdinalIgnoreCase) &&
                    !l.StartsWith(SerieBdUrlPrefix, StringComparison.OrdinalIgnoreCase))
        .ToList();
    if (serieLinks.Count == 1)
    {
        var albumUrl = await ResolveSeriePageAsync(serieLinks[0], normalizedIsbn);
        if (albumUrl is not null) return (albumUrl, false);
    }

    return (null, false);
}

async Task<string?> ResolveSeriePageAsync(string serieUrl, string isbn)
{
    var response = await bedeClient.GetAsync(new Uri(serieUrl), ct);
    if (!response.IsSuccessStatusCode) return null;

    var html = await response.Content.ReadAsStringAsync(ct);
    var doc = new HtmlDocument();
    doc.LoadHtml(html);

    var items = doc.DocumentNode.SelectNodes(
        "//li[@itemscope and @itemtype='https://schema.org/Book']");
    if (items is null) return null;

    foreach (var item in items)
    {
        var isbnSpan = item.SelectSingleNode(".//span[@itemprop='isbn']");
        if (isbnSpan is null) continue;
        var pageIsbn = IsbnHelper.NormalizeIsbn(HtmlEntity.DeEntitize(isbnSpan.InnerText.Trim()));
        if (!pageIsbn.Equals(isbn, StringComparison.OrdinalIgnoreCase)) continue;

        var anchor = item.SelectSingleNode(".//a[@itemprop='url' and contains(@class,'titre')]");
        var href = anchor?.GetAttributeValue("href", null);
        if (!string.IsNullOrEmpty(href)) return href;
    }
    return null;
}

// ─── DTOs for Serper API JSON deserialization ─────────────────────────────────

internal sealed record SerperResponse(
    [property: JsonPropertyName("organic")] IReadOnlyList<SerperOrganicResult>? Organic
);

internal sealed record SerperOrganicResult(
    [property: JsonPropertyName("link")] string? Link
);
