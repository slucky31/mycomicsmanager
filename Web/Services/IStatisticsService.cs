using Application.Statistics.Get;
using Domain.Primitives;

namespace Web.Services;

public interface IStatisticsService
{
    Task<Result<StatisticsDto>> Get(Guid? libraryId, CancellationToken cancellationToken = default);
}
