using System.Runtime.CompilerServices;
using Domain.Books;
using Domain.Primitives;

// Nécessaire pour que l'on puisse utiliser NSubstitute dans les tests unitaires (Web.Tests)
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Web.Services;

public interface IBooksService
{
    Task<Result<Book>> Create(string series, string title, string isbn);
    Task<Result<Book>> Create(string series, string title, string isbn, int volumeNumber);
    Task<Result<Book>> Create(string series, string title, string isbn, int volumeNumber, string imageLink, int rating, CancellationToken cancellationToken = default);
    Task<Result<Book>> Create(string series, string title, string isbn, int volumeNumber, string imageLink, int rating,
        string authors, string publishers, DateOnly? publishDate, int? numberOfPages, CancellationToken cancellationToken = default);
    Task<Result<Book>> GetById(string? id);
    Task<Result<Book>> Update(string? id, string series, string title, string isbn, int volumeNumber, string imageLink, int rating, CancellationToken cancellationToken = default);
    Task<Result<Book>> Update(string? id, string series, string title, string isbn, int volumeNumber, string imageLink, int rating,
        string authors, string publishers, DateOnly? publishDate, int? numberOfPages, CancellationToken cancellationToken = default);
    Task<Result<List<Book>>> GetAll();
    Task<Result> Delete(string? id, CancellationToken cancellationToken = default);
}
