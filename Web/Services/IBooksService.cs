using System.Runtime.CompilerServices;
using Application.Books.Delete;
using Application.Books.List;
using Application.Books.Search;
using Application.Interfaces;
using Domain.Books;
using Domain.Libraries;
using Domain.Primitives;

// Nécessaire pour que l'on puisse utiliser NSubstitute dans les tests unitaires (Web.Tests)
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Web.Services;

public interface IBooksService
{
    Task<Result<Book>> Create(CreateBookRequest request, CancellationToken cancellationToken = default);
    Task<Result<Book>> GetById(string? id);
    Task<Result<Book>> Update(UpdateBookRequest request, CancellationToken cancellationToken = default);
    // Whether the ISBN can be given to the book: false when another book of the same type holds it (#1217).
    Task<Result<bool>> IsIsbnAvailable(string bookId, string isbn, CancellationToken cancellationToken = default);
    Task<Result<List<Book>>> GetAll();
    Task<Result<List<Book>>> GetByLibrary(Guid libraryId, CancellationToken cancellationToken = default);
    Task<Result<IPagedList<BookSummaryDto>>> GetPagedByLibrary(Guid libraryId, int page, int pageSize, BookSortOrder sortOrder, string? searchTerm = null, CancellationToken cancellationToken = default);
    // The series and books of all the libraries whose title or series holds the term (#1209).
    Task<Result<BookSearchResult>> Search(string searchTerm, int bookLimit, CancellationToken cancellationToken = default);
    Task<Result> Delete(string? id, CancellationToken cancellationToken = default);
    // Several books of one library (#1194): those that can be deleted are, the result says how many.
    Task<Result<DeleteBooksResult>> Delete(IReadOnlyList<Guid> bookIds, CancellationToken cancellationToken = default);
    Task<Result<ReadingDate>> AddReadingDate(string bookId, int rating, CancellationToken cancellationToken = default);
    Task<Result> DeleteReadingDate(string bookId, string readingDateId, CancellationToken cancellationToken = default);
}
