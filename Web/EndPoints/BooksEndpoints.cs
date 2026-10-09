using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Books.GetById;
using Application.Books.Read;
using Application.Interfaces;
using Application.Libraries;
using Application.Users;
using Domain.Books;
using Microsoft.Net.Http.Headers;

namespace Web.EndPoints;

internal static class BooksEndpoints
{
    internal static void RegisterBooksEndpoints(this WebApplication app)
    {
        app.MapGet("/api/books/{bookId}/download", DownloadBookAsync).RequireAuthorization();
        app.MapGet("/api/books/{bookId:guid}/pages/{pageIndex:int}", GetBookPageAsync).RequireAuthorization();
    }

    private static async Task<IResult> DownloadBookAsync(
        Guid bookId,
        ClaimsPrincipal user,
        IQueryHandler<GetBookByIdQuery, Book> getBookHandler,
        IUserReadService userReadService,
        ILibraryLocalStorage libraryStorage,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(user, userReadService, ct);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var query = new GetBookByIdQuery(bookId, userId.Value);
        var result = await getBookHandler.Handle(query, ct);

        if (result.IsFailure || result.Value is null)
        {
            return Results.NotFound();
        }

        if (result.Value is not DigitalBook digitalBook)
        {
            return Results.BadRequest("Only digital books can be downloaded.");
        }

        var normalizedRoot = Path.GetFullPath(libraryStorage.rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedFilePath = Path.GetFullPath(digitalBook.FilePath);
        if (!normalizedFilePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Forbid();
        }

        if (!File.Exists(digitalBook.FilePath))
        {
            return Results.NotFound("File not found on server.");
        }

        var fileName = Path.GetFileName(digitalBook.FilePath);
        // Ownership is enforced by GetBookByIdQuery filtering on userId.
        return Results.File(digitalBook.FilePath, "application/x-cbz", fileName, enableRangeProcessing: true);
    }

    private static async Task<IResult> GetBookPageAsync(
        Guid bookId,
        int pageIndex,
        ClaimsPrincipal user,
        HttpContext httpContext,
        IQueryHandler<GetBookPageQuery, ComicPage> getPageHandler,
        IUserReadService userReadService,
        CancellationToken ct)
    {
        var userId = await ResolveUserIdAsync(user, userReadService, ct);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        // Ownership is enforced by GetBookPageQuery filtering on userId.
        var result = await getPageHandler.Handle(new GetBookPageQuery(bookId, userId.Value, pageIndex), ct);
        if (result.IsFailure || result.Value is null)
        {
            return result.Error == BooksError.BadRequest ? Results.BadRequest() : Results.NotFound();
        }

        // The page is decompressed in memory on each request and never stored on the server:
        // the browser keeps it, and the ETag changes as soon as the archive file is replaced.
        var page = result.Value;
        httpContext.Response.Headers.CacheControl = "private, max-age=86400";
        var etag = new EntityTagHeaderValue($"\"{page.LastModifiedUtc.Ticks:x}-{pageIndex}\"");
        return Results.Bytes(page.Content, page.ContentType, lastModified: page.LastModifiedUtc, entityTag: etag);
    }

    // Resolves the application user id from the Auth0 "sub" claim, falling back to the email
    // for users not yet migrated to sub-based AuthId. Returns null when the user is unknown.
    private static async Task<Guid?> ResolveUserIdAsync(ClaimsPrincipal user, IUserReadService userReadService, CancellationToken ct)
    {
        var sub = user.FindFirstValue("sub")
               ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(sub))
        {
            var byAuthId = await userReadService.GetUserByAuthId(sub, ct);
            if (byAuthId.IsSuccess)
            {
                return byAuthId.Value!.Id;
            }
        }

        var email = user.Identity?.Name;
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        var byEmail = await userReadService.GetUserByEmail(email, ct);
        return byEmail.IsSuccess ? byEmail.Value!.Id : null;
    }
}
