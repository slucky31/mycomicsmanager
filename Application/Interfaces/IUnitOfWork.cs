using Domain.Primitives;

namespace Application.Interfaces;

public interface IUnitOfWork
{
    Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken);
}
