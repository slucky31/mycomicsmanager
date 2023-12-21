using Domain.Primitives;

namespace Application.Data;

public interface IUnitOfWork
{
    Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken);
}
