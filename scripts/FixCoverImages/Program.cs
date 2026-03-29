/*
 * FixCoverImages — one-shot tool
 * ------------------------------------
 * Usage:
 *   dotnet run --project scripts/FixCoverImages -- \
 *       --connection "Host=...;Database=...;Username=...;Password=..." \
 *       --cloudinary-cloud "your-cloud-name" \
 *       --cloudinary-key "your-api-key" \
 *       --cloudinary-secret "your-api-secret" \
 *       [--cloudinary-folder "covers"] \
 *       [--delay 500] \
 *       [--dry-run]
 *
 * Environment variables can be used instead of CLI args:
 *   ConnectionStrings__Default
 *   Cloudinary__CloudName
 *   Cloudinary__ApiKey
 *   Cloudinary__ApiSecret
 *   Cloudinary__Folder  (default: "covers")
 *
 * The tool will:
 *   1. Query all books whose ImageLink is not empty and not already a Cloudinary URL
 *   2. For each book, upload the image to Cloudinary using the book's ISBN as publicId
 *   3. Update the book's ImageLink in the database with the Cloudinary URL
 *      (or the original URL if the upload fails)
 */

using Application;
using Application.ComicInfoSearch;
using Application.Helpers;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;

// ─── Arg parsing ──────────────────────────────────────────────────────────────

string? connectionString   = null;
string? cloudinaryCloud    = null;
string? cloudinaryKey      = null;
string? cloudinarySecret   = null;
string? cloudinaryFolder   = null;
int     delayMs            = 500;
bool    dryRun             = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--connection":        connectionString = args[++i]; break;
        case "--cloudinary-cloud":  cloudinaryCloud  = args[++i]; break;
        case "--cloudinary-key":    cloudinaryKey    = args[++i]; break;
        case "--cloudinary-secret": cloudinarySecret = args[++i]; break;
        case "--cloudinary-folder": cloudinaryFolder = args[++i]; break;
        case "--delay":             delayMs          = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture); break;
        case "--dry-run":           dryRun           = true; break;
    }
}

// ─── Configuration ────────────────────────────────────────────────────────────

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddInMemoryCollection(BuildCloudinaryOverrides(cloudinaryCloud, cloudinaryKey, cloudinarySecret, cloudinaryFolder))
    .Build();

connectionString ??= config.GetConnectionString("Default");

if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("ERROR: --connection or ConnectionStrings__Default env var is required.");
    return 1;
}

var cloudName   = cloudinaryCloud   ?? config["Cloudinary:CloudName"];
var apiKey      = cloudinaryKey     ?? config["Cloudinary:ApiKey"];
var apiSecret   = cloudinarySecret  ?? config["Cloudinary:ApiSecret"];

if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
{
    Console.Error.WriteLine("ERROR: Cloudinary credentials are required (--cloudinary-cloud/key/secret or Cloudinary__* env vars).");
    return 1;
}

if (dryRun)
    Console.WriteLine("DRY RUN — no changes will be saved.");

// ─── DI setup ─────────────────────────────────────────────────────────────────

var services = new ServiceCollection();
services.AddApplication();
services.AddInfrastructure(connectionString, rootPath: Path.GetTempPath(), config);

var provider = services.BuildServiceProvider();

// ─── Step 1 : Query books with non-Cloudinary cover ──────────────────────────

const string CloudinaryHost = "res.cloudinary.com";

List<(Guid Id, string ISBN, string ImageLink)> books;
using (var scope = provider.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var rawBooks = await context.Books
        .Where(b => !string.IsNullOrEmpty(b.ImageLink) &&
                    !b.ImageLink.Contains(CloudinaryHost))
        .Select(b => new { b.Id, b.ISBN, b.ImageLink })
        .ToListAsync();
    books = rawBooks.Select(b => (b.Id, b.ISBN, b.ImageLink)).ToList();
}

Console.WriteLine($"{books.Count} book(s) with non-Cloudinary covers to process.");

if (books.Count == 0)
{
    Console.WriteLine("Nothing to do.");
    return 0;
}

// ─── Step 2 : Upload each cover to Cloudinary ────────────────────────────────

int updated = 0, skipped = 0, failed = 0;
var ct = CancellationToken.None;

for (int i = 0; i < books.Count; i++)
{
    var (bookId, isbn, imageLink) = books[i];
    var prefix = $"[{i + 1,4}/{books.Count}] {bookId}";

    if (!Uri.TryCreate(imageLink, UriKind.Absolute, out var imageUri))
    {
        Console.WriteLine($"{prefix} → SKIP (not a valid URL): {imageLink}");
        skipped++;
        continue;
    }

    try
    {
        using var scope = provider.CreateScope();
        var cloudinaryService = scope.ServiceProvider.GetRequiredService<ICloudinaryService>();
        var cloudinarySettings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudinarySettings>>().Value;

        var publicId = string.IsNullOrEmpty(isbn)
            ? bookId.ToString()
            : IsbnHelper.NormalizeIsbn(isbn);

        if (dryRun)
        {
            Console.WriteLine($"{prefix} → DRY-RUN would upload {imageLink} as '{publicId}'");
            updated++;
        }
        else
        {
            var result = await cloudinaryService.UploadImageFromUrlAsync(
                imageUri,
                cloudinarySettings.Folder,
                publicId,
                ct);

            if (result.Success && result.Url != null)
            {
                var newUrl = result.Url.ToString();

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Books
                    .Where(b => b.Id == bookId)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.ImageLink, newUrl), ct);

                Console.WriteLine($"{prefix} → {newUrl}");
                updated++;
            }
            else
            {
                Console.Error.WriteLine($"{prefix} → UPLOAD FAILED: {result.Error} (kept original URL)");
                failed++;
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"{prefix} → EXCEPTION: {ex.Message}");
        failed++;
    }

    if (i < books.Count - 1)
        await Task.Delay(delayMs, ct);
}

// ─── Summary ──────────────────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine($"Done — updated: {updated}, skipped: {skipped}, failed: {failed}");
return failed > 0 ? 2 : 0;

// ─── Helpers ──────────────────────────────────────────────────────────────────

static IEnumerable<KeyValuePair<string, string?>> BuildCloudinaryOverrides(
    string? cloudName, string? apiKey, string? apiSecret, string? folder)
{
    if (cloudName != null) yield return new("Cloudinary:CloudName", cloudName);
    if (apiKey    != null) yield return new("Cloudinary:ApiKey",    apiKey);
    if (apiSecret != null) yield return new("Cloudinary:ApiSecret", apiSecret);
    if (folder    != null) yield return new("Cloudinary:Folder",    folder);
}
