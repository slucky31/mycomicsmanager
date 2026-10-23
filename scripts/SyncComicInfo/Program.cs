/*
 * SyncComicInfo — one-shot tool
 * ------------------------------------
 * Usage:
 *   dotnet run --project scripts/SyncComicInfo -- \
 *       --connection "Host=...;Database=...;Username=...;Password=..." \
 *       --root "/path/to/libraries" \
 *       [--dry-run]
 *
 * The ConnectionStrings__Default and LocalStorage__RootPath env vars can be used instead of
 * --connection and --root respectively.
 *
 * The tool writes the metadata of every digital book (as stored in the database) into the
 * ComicInfo.xml of its CBZ, the same way saving a book does since the edit keeps them in sync:
 *   - Title, Series, Number/Volume, Year/Month/Day, Writer, Publisher and ISBN are set, or removed
 *     when the book has no value;
 *   - PageCount is set when the book knows it;
 *   - every other element of the existing ComicInfo.xml (Summary, Penciller, Genre...) is kept.
 *
 * The book file paths are read from the database: the tool must run where those paths exist
 * (the same mount point as the application), and --root must be the application's LocalStorage:RootPath.
 */

using Application;
using Application.Books;
using Application.Interfaces;
using Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;

// ─── Arg parsing ──────────────────────────────────────────────────────────────

string? connectionString = null;
string? rootPath = null;
var dryRun = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--connection":
            connectionString = args[++i];
            break;
        case "--root":
            rootPath = args[++i];
            break;
        case "--dry-run":
            dryRun = true;
            break;
    }
}

// ─── Configuration ────────────────────────────────────────────────────────────

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

connectionString ??= config.GetConnectionString("Default");
rootPath ??= config["LocalStorage:RootPath"];

if (string.IsNullOrEmpty(connectionString))
{
    await Console.Error.WriteLineAsync("ERROR: --connection or ConnectionStrings__Default env var is required.");
    return 1;
}

if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
{
    await Console.Error.WriteLineAsync("ERROR: --root or LocalStorage__RootPath env var must name an existing folder.");
    return 1;
}

if (dryRun)
{
    Console.WriteLine("DRY RUN — no archive will be changed.");
}

// ─── DI setup ─────────────────────────────────────────────────────────────────

var services = new ServiceCollection();
services.AddLogging();
services.AddApplication();
services.AddInfrastructure(connectionString, rootPath, config);

await using var provider = services.BuildServiceProvider();

// ─── Step 1 : Query the digital books ────────────────────────────────────────

List<DigitalBook> books;
using (var scope = provider.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    books = await context.DigitalBooks.AsNoTracking().OrderBy(b => b.FilePath).ToListAsync();
}

Console.WriteLine($"{books.Count} digital book(s) to process.");

// ─── Step 2 : Write the ComicInfo.xml of each archive ────────────────────────

int updated = 0, failed = 0;
using var writerScope = provider.CreateScope();
var writer = writerScope.ServiceProvider.GetRequiredService<IComicInfoArchiveWriter>();

for (var i = 0; i < books.Count; i++)
{
    var book = books[i];
    var prefix = $"[{i + 1,4}/{books.Count}] {book.Serie} T{book.VolumeNumber} - {book.Title}";

    if (dryRun)
    {
        var exists = File.Exists(book.FilePath);
        Console.WriteLine($"{prefix} → DRY-RUN {(exists ? "would update" : "FILE NOT FOUND")}: {book.FilePath}");
        if (exists) { updated++; } else { failed++; }
        continue;
    }

    var result = await writer.WriteAsync(book.FilePath, BookComicInfo.From(book));
    if (result.IsSuccess)
    {
        Console.WriteLine($"{prefix} → OK");
        updated++;
    }
    else
    {
        await Console.Error.WriteLineAsync($"{prefix} → FAILED ({result.Error?.Description}): {book.FilePath}");
        failed++;
    }
}

Console.WriteLine();
Console.WriteLine($"Done: {updated} updated, {failed} failed.");
return failed == 0 ? 0 : 2;
