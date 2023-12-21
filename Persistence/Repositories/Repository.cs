using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using Domain.Dto;
using Application.Interfaces;


namespace Persistence.Repositories;
public class Repository<TEntity, TEntityId> : IRepository<TEntity, TEntityId>
    where TEntity : EntityDto
    where TEntityId : class
{
    private readonly ApplicationDbContext DbContext;

    public Repository(ApplicationDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<TEntity?> GetByIdAsync(TEntityId id)
    {
        // TODO : check if id = null
        
        var objId = new ObjectId(id.ToString());
        return await DbContext.Set<TEntity>().SingleOrDefaultAsync(p => p.Id == objId);
    }

    public async Task<List<TEntity>> GetListAsync()
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
