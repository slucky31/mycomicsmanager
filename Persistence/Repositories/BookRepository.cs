using Application.Interfaces;
using Application.Helpers;
using Ardalis.GuardClauses;
using Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class BookRepository(ApplicationDbContext dbContext) : IBookRepository
{
    public async Task<Book?> GetByIdAsync(Guid id)
    {
        // Guid is a value type and cannot be null.
        // If you want to guard against the default value (Guid.Empty), use Guard.Against.Default(id) instead.
        Guard.Against.Default(id);

        return await dbContext.Set<Book>()
            .Include(b => b.ReadingDates)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public void Add(Book entity)
    {
        dbContext.Set<Book>().Add(entity);
    }

    public void Update(Book entity)
    {
        dbContext.Set<Book>().Update(entity);
    }

    public void Remove(Book entity)
    {
        dbContext.Set<Book>().Remove(entity);
    }

    public int Count()
    {
        return dbContext.Set<Book>().Count();
    }

    public async Task<List<Book>> ListAsync()
    {
        return await ListAsync(CancellationToken.None);
    }

    public async Task<List<Book>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<Book>()
            .Include(b => b.ReadingDates)
            .ToListAsync(cancellationToken);
    }

    public async Task<Book?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            throw new ArgumentException("ISBN cannot be null or empty.", nameof(isbn));
        }

        var normalizedIsbn = IsbnHelper.NormalizeIsbn(isbn);

        return await dbContext.Books
            .FirstOrDefaultAsync(b => b.ISBN == normalizedIsbn, cancellationToken);
    }
}
