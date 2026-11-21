using Application.Statistics.GetStatistics;
using Domain.Primitives;

namespace Web.Services;

public interface IStatisticsService
{
    Task<Result<StatisticsDto>> GetStatistics(Guid? libraryId, CancellationToken cancellationToken = default);
}
