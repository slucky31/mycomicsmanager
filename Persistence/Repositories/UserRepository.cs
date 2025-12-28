using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;
public class UserRepository(ApplicationDbContext dbContext) : IRepository<User, Guid>
{
    public async Task<User?> GetByIdAsync(Guid id)
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
