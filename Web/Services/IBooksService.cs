using System.Runtime.CompilerServices;
using Application.Books.List;
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
    Task<Result<List<Book>>> GetAll();
    Task<Result<List<Book>>> GetByLibrary(Guid libraryId, CancellationToken cancellationToken = default);
    Task<Result<IPagedList<BookSummaryDto>>> GetPagedByLibrary(Guid libraryId, int page, int pageSize, BookSortOrder sortOrder, string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<Result> Delete(string? id, CancellationToken cancellationToken = default);
    Task<Result<ReadingDate>> AddReadingDate(string bookId, int rating, CancellationToken cancellationToken = default);
    Task<Result> DeleteReadingDate(string bookId, string readingDateId, CancellationToken cancellationToken = default);
}
