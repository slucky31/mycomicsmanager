using Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;
public class Repository<TEntity, TEntityId> : IRepository<TEntity, TEntityId>
    where TEntity : Entity<TEntityId>
    where TEntityId : class
{
    private readonly ApplicationDbContext DbContext;

    public Repository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<TEntity?> GetByIdAsync(TEntityId id)
    {
        return await DbContext.Set<TEntity>().SingleOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<TEntity>> GetAllAsync()
    {
        return await DbContext.Set<TEntity>().ToListAsync();
    }

    public void Add(TEntity entity)
    {
        DbContext.Set<TEntity>().Add(entity);
    }

    public void Update(TEntity entity)
    {
        DbContext.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
    }

}
