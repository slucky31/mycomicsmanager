namespace Domain.Primitives;

public interface IRepository<TEntity, TEntityId>
{
    public Task<TEntity?> GetByIdAsync(TEntityId id);

    public Task<List<TEntity>> GetAllAsync();

    void Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
