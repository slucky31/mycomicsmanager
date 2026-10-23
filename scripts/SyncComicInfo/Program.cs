/*
 * SyncComicInfo — one-shot tool
 * ------------------------------------
 * The book file paths are read from the database: the tool must see them as the application does,
 * so it runs inside the application's container, which already holds the ConnectionStrings__DefaultConnection
 * and LocalStorage__RootPath env vars (--connection and --root override them):
 *
 *   dotnet publish scripts/SyncComicInfo -c Release -r linux-arm64 --self-contained false -o ./sync-comicinfo
 *     (linux-x64 on an x64 host; without -r the native libraries of every platform make it ~500 MB)
 *   docker cp ./sync-comicinfo <container>:/tmp/sync-comicinfo
 *   docker exec <container> dotnet /tmp/sync-comicinfo/SyncComicInfo.dll --dry-run
 *   docker exec <container> dotnet /tmp/sync-comicinfo/SyncComicInfo.dll
 *   docker exec <container> rm -rf /tmp/sync-comicinfo
 *
 * The tool writes the metadata of every digital book (as stored in the database) into the
 * ComicInfo.xml of its CBZ, the same way saving a book does since the edit keeps them in sync:
 *   - Title, Series, Number/Volume, Year/Month/Day, Writer, Publisher and ISBN are set, or removed
 *     when the book has no value;
 *   - PageCount is set when the book knows it;
 *   - every other element of the existing ComicInfo.xml (Summary, Penciller, Genre...) is kept.
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

connectionString ??= config.GetConnectionString("DefaultConnection");
rootPath ??= config["LocalStorage:RootPath"];

if (string.IsNullOrEmpty(connectionString))
{
    await Console.Error.WriteLineAsync("ERROR: --connection or ConnectionStrings__DefaultConnection env var is required.");
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
