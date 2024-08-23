using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace Persistence.Repositories;
public class UserRepository(ApplicationDbContext dbContext) : IRepository<User, ObjectId>
{
    public async Task<User?> GetByIdAsync(ObjectId id)
    {
        Guard.Against.Null(id);
        return await dbContext.Set<User>().SingleOrDefaultAsync(p => p.Id == id);
    }

    public void Add(User entity)
    {
        dbContext.Set<User>().Add(entity);
    }

    public void Update(User entity)
    {
        dbContext.Set<User>().Update(entity);
    }

    public void Remove(User entity)
    {
        dbContext.Set<User>().Remove(entity);
    }

    public int Count()
    {
        return dbContext.Set<User>().Count();
    }

    public async Task<List<User>> ListAsync()
    {
        return await dbContext.Set<User>().ToListAsync();
    }
}
