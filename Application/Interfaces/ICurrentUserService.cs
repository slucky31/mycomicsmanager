using Domain.Primitives;

namespace Application.Interfaces;

public interface ICurrentUserService
{
    Task<Result<Guid>> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
}
