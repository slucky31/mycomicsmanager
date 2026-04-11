using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Books.GetById;
using Application.Users;
using Domain.Books;

namespace Web.EndPoints;

internal static class BooksEndpoints
{
    internal static void RegisterBooksEndpoints(this WebApplication app)
    {
        app.MapGet("/api/books/{bookId}/download", async (
            Guid bookId,
            ClaimsPrincipal user,
            IQueryHandler<GetBookByIdQuery, Book> getBookHandler,
            IUserReadService userReadService,
            CancellationToken ct) =>
        {
            var sub = user.FindFirstValue("sub")
                   ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            var userId = Guid.Empty;
            if (!string.IsNullOrEmpty(sub))
            {
                var byAuthId = await userReadService.GetUserByAuthId(sub, ct);
                if (byAuthId.IsSuccess)
                {
                    userId = byAuthId.Value!.Id;
                }
            }

            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var query = new GetBookByIdQuery(bookId, userId);
            var result = await getBookHandler.Handle(query, ct);

            if (result.IsFailure || result.Value is null)
            {
                return Results.NotFound();
            }

            if (result.Value is not DigitalBook digitalBook)
            {
                return Results.BadRequest("Only digital books can be downloaded.");
            }

            if (!File.Exists(digitalBook.FilePath))
            {
                return Results.NotFound("File not found on server.");
            }

            var fileName = Path.GetFileName(digitalBook.FilePath);
            var stream = File.OpenRead(digitalBook.FilePath);
            return Results.File(stream, "application/x-cbz", fileName, enableRangeProcessing: true);
        }).RequireAuthorization();
    }
}
