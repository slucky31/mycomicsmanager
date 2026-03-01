using Application.Helpers;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Books;
using Domain.Libraries;
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
        dbContext.Entry(entity).State = EntityState.Modified;
    }

    public void AddReadingDate(ReadingDate readingDate)
    {
        dbContext.Set<ReadingDate>().Add(readingDate);
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

    public async Task<List<Book>> ListByLibraryIdAsync(Guid libraryId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Book>()
            .Include(b => b.ReadingDates)
            .Where(b => b.LibraryId == libraryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Book>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<Book>()
            .Include(r => r.ReadingDates)
            .Join(dbContext.Set<Library>(), b => b.LibraryId, l => l.Id, (b, l) => new { b, l.UserId })            
            .Where(x => x.UserId == userId)
            .Select(x => x.b)
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
