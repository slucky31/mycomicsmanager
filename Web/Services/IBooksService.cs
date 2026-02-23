using System.Runtime.CompilerServices;
using Domain.Books;
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
    Task<Result> Delete(string? id, CancellationToken cancellationToken = default);
    Task<Result<ReadingDate>> AddReadingDate(string bookId, int rating, CancellationToken cancellationToken = default);
    Task<Result> DeleteReadingDate(string bookId, string readingDateId, CancellationToken cancellationToken = default);
}
