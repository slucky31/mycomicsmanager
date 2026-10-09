using Application.Abstractions.Messaging;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.Books.Read;

public sealed class GetBookPageQueryHandler(
    IBookRepository bookRepository,
    IRepository<Library, Guid> libraryRepository,
    IComicPageReader pageReader) : IQueryHandler<GetBookPageQuery, ComicPage>
{
    public async Task<Result<ComicPage>> Handle(GetBookPageQuery request, CancellationToken cancellationToken)
    {
        if (request is null || request.BookId == Guid.Empty || request.UserId == Guid.Empty)
        {
            return BooksError.BadRequest;
        }

        if (request.PageIndex < 0)
        {
            return BooksError.PageNotFound;
        }

        var bookResult = await OwnedDigitalBook.GetAsync(bookRepository, libraryRepository, request.BookId, request.UserId);
        if (bookResult.IsFailure)
        {
            return bookResult.Error!;
        }

        return await pageReader.ReadPageAsync(bookResult.Value!.FilePath, request.PageIndex, cancellationToken);
    }
}
