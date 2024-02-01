using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Persistence.Repositories;
public class LibraryRepository : IRepository<Library, ObjectId>
{
    private readonly ApplicationDbContext DbContext;

    public LibraryRepository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<Library?> GetByIdAsync(ObjectId id)
    {        
        Guard.Against.Null(id);        
        return await DbContext.Set<Library>().SingleOrDefaultAsync(p => p.Id == id);
    }

    public void Add(Library entity)
    {
        DbContext.Set<Library>().Add(entity);
    }

    public void Update(Library entity)
    {
        DbContext.Set<Library>().Update(entity);
    }

    public void Remove(Library entity)
    {
        DbContext.Set<Library>().Remove(entity);
    }

    public async Task<List<Library>> GetListAsync()
    {
        return await DbContext.Set<Library>().ToListAsync();
    }
}
