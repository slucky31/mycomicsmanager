namespace Application.Interfaces;

public interface IRepository<TEntity, TEntityId>
{
    Task<TEntity?> GetByIdAsync(TEntityId id);

    void Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    Task<List<TEntity>> GetListAsync();
    
}

