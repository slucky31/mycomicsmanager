using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IRepository<TEntity, TEntityId>
{
    public Task<TEntity?> GetByIdAsync(TEntityId id);

    void Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    public Task<List<TEntity>> GetListAsync();
    
}

