/*
 * ImportBooks — one-shot import tool
 * ------------------------------------
 * Usage:
 *   dotnet run --project scripts/ImportBooks -- \
 *       --file "C:\path\to\books.json" \
 *       --user-id "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" \
 *       --connection "Host=...;Database=...;Username=...;Password=..."
 *
 * The ConnectionStrings__Default env var can be used instead of --connection.
 *
 * The tool will:
 *   1. Create (or reuse) the "Read Books" library for the user
 *   2. Import all books from the JSON file as PhysicalBooks
 *   3. Set the reading date to Added.$date from the JSON (historical import)
 *   4. Skip books whose ISBN already exists in the database
 */

using System.Text.Json;
using Application;
using Application.Books.Create;
using Application.ComicInfoSearch;
using Application.Interfaces;
using Application.Libraries.Create;
using Application.Libraries.List;
using Application.Abstractions.Messaging;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;

// ─── Arg parsing ──────────────────────────────────────────────────────────────

string? jsonFile         = null;
Guid    userId           = Guid.Empty;
string? connectionString = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--file":       jsonFile         = args[++i]; break;
        case "--user-id":    userId           = Guid.Parse(args[++i]); break;
        case "--connection": connectionString = args[++i]; break;
    }
}

// ─── Validate ─────────────────────────────────────────────────────────────────

if (string.IsNullOrEmpty(jsonFile) || !File.Exists(jsonFile))
{
    Console.Error.WriteLine("ERROR: --file <path> is required and must exist.");
    return 1;
}

if (userId == Guid.Empty)
{
    Console.Error.WriteLine("ERROR: --user-id <guid> is required.");
    return 1;
}

var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

connectionString ??= config.GetConnectionString("Default");

if (string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("ERROR: --connection or ConnectionStrings__Default env var is required.");
    return 1;
}

// ─── DI setup ─────────────────────────────────────────────────────────────────

var services = new ServiceCollection();
services.AddApplication();
services.AddInfrastructure(connectionString, rootPath: Path.GetTempPath(), config);

// Google Books (no API key — public endpoint)
services.AddOptions<GoogleBooksSettings>()
    .Configure(o => o.BaseUrl = new Uri("https://www.googleapis.com/books/v1/"));
services.AddHttpClient<IGoogleBooksService, GoogleBooksService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "MyComicsManager/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var provider = services.BuildServiceProvider();

// ─── Step 1 : Get or create "Read Books" library ──────────────────────────────

const string DefaultLibraryName = "Read Books";
Console.WriteLine($"User  : {userId}");
Console.WriteLine($"File  : {jsonFile}");
Console.WriteLine($"Looking for library \"{DefaultLibraryName}\"…");

var listHandler = provider
    .GetRequiredService<IQueryHandler<GetLibrariesQuery, IPagedList<Library>>>();

var listResult = await listHandler.Handle(
    new GetLibrariesQuery(DefaultLibraryName, null, null, 1, 10, userId),
    CancellationToken.None);

Library? library = null;

if (listResult.IsSuccess)
    library = listResult.Value!.Items.FirstOrDefault(l =>
        l.Name.Equals(DefaultLibraryName, StringComparison.OrdinalIgnoreCase));

if (library is null)
{
    Console.WriteLine($"Not found — creating \"{DefaultLibraryName}\"…");
    var createLibHandler = provider
        .GetRequiredService<ICommandHandler<CreateLibraryCommand, Library>>();

    var createLibResult = await createLibHandler.Handle(
        new CreateLibraryCommand(DefaultLibraryName, "#607D8B", "book", LibraryBookType.Physical, userId),
        CancellationToken.None);

    if (createLibResult.IsFailure)
    {
        Console.Error.WriteLine($"ERROR creating library: {createLibResult.Error}");
        return 1;
    }

    library = createLibResult.Value!;
}

Console.WriteLine($"Library: \"{library.Name}\" ({library.Id})");
Console.WriteLine();

// ─── Step 2 : Parse JSON ──────────────────────────────────────────────────────

var json = await File.ReadAllTextAsync(jsonFile);
var docs = JsonSerializer.Deserialize<List<BookDoc>>(json,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (docs is null || docs.Count == 0)
{
    Console.Error.WriteLine("ERROR: No records found in JSON file.");
    return 1;
}

Console.WriteLine($"{docs.Count} records to process.");

// ─── Step 3 : Import each book ────────────────────────────────────────────────

int imported = 0, skipped = 0, failed = 0;

foreach (var doc in docs)
{
    using var scope = provider.CreateScope();
    var sp = scope.ServiceProvider;

    var createBookHandler = sp.GetRequiredService<ICommandHandler<CreateBookCommand, Book>>();

    // Rating must be 1-5; clamp values from the source data
    var rating = Math.Clamp(doc.Review, 1, 5);

    // Serie is required by the domain — fall back to Title for standalone books
    var serie = string.IsNullOrWhiteSpace(doc.Serie) ? doc.Title : doc.Serie;

    var command = new CreateBookCommand(
        Serie:         serie                ?? string.Empty,
        Title:         doc.Title           ?? string.Empty,
        ISBN:          doc.Isbn            ?? string.Empty,
        LibraryId:     library.Id,
        UserId:        userId,
        VolumeNumber:  doc.Volume,
        ImageLink:     string.Empty,
        Rating:        rating,
        Authors:       string.Empty,
        Publishers:    string.Empty,
        PublishDate:   null,
        NumberOfPages: null);

    var result = await createBookHandler.Handle(command, CancellationToken.None);

    if (result.IsSuccess)
    {
        // The handler created a ReadingDate with DateTime.UtcNow.
        // Update it to the historical date from Added.$date if available.
        if (doc.Added?.Date is DateTime readAt)
        {
            var bookRepo   = sp.GetRequiredService<IBookRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var savedBook = await bookRepo.GetByIdAsync(result.Value!.Id);
            var rd = savedBook?.ReadingDates.FirstOrDefault();
            if (rd is not null)
            {
                rd.Update(readAt.ToUniversalTime(), rating);
                bookRepo.Update(savedBook!);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
        }

        Console.WriteLine($"  ✓ [{imported + 1,4}] {doc.Serie} – {doc.Title} (vol. {doc.Volume})");
        imported++;
    }
    else if (result.Error == BooksError.Duplicate)
    {
        // Book already exists — add the historical reading date on top of existing ones
        if (doc.Added?.Date is DateTime readAtDup)
        {
            var bookRepo   = sp.GetRequiredService<IBookRepository>();
            var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            var existing = await bookRepo.GetByIsbnAsync(doc.Isbn ?? string.Empty, CancellationToken.None);
            if (existing is not null)
            {
                var rd = existing.AddReadingDate(readAtDup.ToUniversalTime(), rating);
                bookRepo.AddReadingDate(rd);
                bookRepo.Update(existing);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
                Console.WriteLine($"  ~        {doc.Serie} – {doc.Title}: already exists, reading date added");
            }
            else
            {
                Console.WriteLine($"  ~        {doc.Serie} – {doc.Title}: already exists, skipped");
            }
        }
        else
        {
            Console.WriteLine($"  ~        {doc.Serie} – {doc.Title}: already exists, skipped (no date in JSON)");
        }
        skipped++;
    }
    else
    {
        // Attempt enrichment via Google Books then retry
        Console.WriteLine($"  ? [{doc.Serie} – {doc.Title}] failed ({result.Error}), trying Google Books…");

        var googleBooks = sp.GetRequiredService<IGoogleBooksService>();
        var gbResult    = await googleBooks.SearchByIsbnAsync(doc.Isbn ?? string.Empty, CancellationToken.None);

        if (!gbResult.Found)
        {
            Console.Error.WriteLine($"  ✗        {doc.Serie} – {doc.Title}: not found on Google Books either");
            failed++;
        }
        else
        {
            var enrichedCommand = new CreateBookCommand(
                Serie:         string.IsNullOrWhiteSpace(doc.Serie) ? gbResult.Title : doc.Serie,
                Title:         gbResult.Title,
                ISBN:          doc.Isbn                                             ?? string.Empty,
                LibraryId:     library.Id,
                UserId:        userId,
                VolumeNumber:  doc.Volume,
                ImageLink:     gbResult.CoverUrl?.ToString()                       ?? string.Empty,
                Rating:        rating,
                Authors:       string.Join(", ", gbResult.Authors),
                Publishers:    string.Join(", ", gbResult.Publishers),
                PublishDate:   gbResult.PublishDate,
                NumberOfPages: gbResult.NumberOfPages);

            var retry = await createBookHandler.Handle(enrichedCommand, CancellationToken.None);

            if (retry.IsSuccess)
            {
                if (doc.Added?.Date is DateTime readAt2)
                {
                    var bookRepo2   = sp.GetRequiredService<IBookRepository>();
                    var unitOfWork2 = sp.GetRequiredService<IUnitOfWork>();
                    var savedBook2  = await bookRepo2.GetByIdAsync(retry.Value!.Id);
                    var rd2         = savedBook2?.ReadingDates.FirstOrDefault();
                    if (rd2 is not null)
                    {
                        rd2.Update(readAt2.ToUniversalTime(), rating);
                        bookRepo2.Update(savedBook2!);
                        await unitOfWork2.SaveChangesAsync(CancellationToken.None);
                    }
                }

                Console.WriteLine($"  ✓ [{imported + 1,4}] {doc.Serie} – {gbResult.Title} (vol. {doc.Volume}) [enriched]");
                imported++;
            }
            else if (retry.Error == BooksError.Duplicate)
            {
                if (doc.Added?.Date is DateTime readAtDup2)
                {
                    var bookRepo3   = sp.GetRequiredService<IBookRepository>();
                    var unitOfWork3 = sp.GetRequiredService<IUnitOfWork>();

                    var existing = await bookRepo3.GetByIsbnAsync(doc.Isbn ?? string.Empty, CancellationToken.None);
                    if (existing is not null)
                    {
                        var rd3 = existing.AddReadingDate(readAtDup2.ToUniversalTime(), rating);
                        bookRepo3.AddReadingDate(rd3);
                        bookRepo3.Update(existing);
                        await unitOfWork3.SaveChangesAsync(CancellationToken.None);
                        Console.WriteLine($"  ~        {doc.Serie} – {doc.Title}: already exists, reading date added");
                    }
                    else
                    {
                        Console.WriteLine($"  ~        {doc.Serie} – {doc.Title}: already exists, skipped");
                    }
                }
                else
                {
                    Console.WriteLine($"  ~        {doc.Serie} – {doc.Title}: already exists, skipped (no date in JSON)");
                }
                skipped++;
            }
            else
            {
                Console.Error.WriteLine($"  ✗        {doc.Serie} – {doc.Title}: {retry.Error}");
                failed++;
            }
        }
    }
}

// ─── Summary ──────────────────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine($"Done — imported: {imported}, skipped: {skipped}, failed: {failed}");
return failed > 0 ? 2 : 0;

// ─── JSON model ───────────────────────────────────────────────────────────────

/// <summary>Matches the MongoDB export shape.</summary>
internal sealed class BookDoc
{
    public string?   Serie  { get; set; }
    public string?   Title  { get; set; }
    public string?   Isbn   { get; set; }
    public int       Volume { get; set; } = 1;
    public int       Review { get; set; }

    // MongoDB date wrapper: { "$date": "2022-01-23T21:43:26.513Z" }
    public AddedDoc? Added  { get; set; }
}

internal sealed class AddedDoc
{
    [System.Text.Json.Serialization.JsonPropertyName("$date")]
    public DateTime? Date { get; set; }
}
