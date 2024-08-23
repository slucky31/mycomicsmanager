using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Persistence.Repositories;
public class LibraryRepository(ApplicationDbContext dbContext) : IRepository<Library, ObjectId>
{
    public async Task<Library?> GetByIdAsync(ObjectId id)
    {
        Guard.Against.Null(id);
        return await dbContext.Set<Library>().SingleOrDefaultAsync(p => p.Id == id);
    }

    public void Add(Library entity)
    {
        dbContext.Set<Library>().Add(entity);
    }

    public void Update(Library entity)
    {
        dbContext.Set<Library>().Update(entity);
    }

    public void Remove(Library entity)
    {
        dbContext.Set<Library>().Remove(entity);
    }

    public int Count()
    {
        return dbContext.Set<Library>().Count();
    }

    public async Task<List<Library>> ListAsync()
    {
        return await dbContext.Set<Library>().ToListAsync();
    }
}
