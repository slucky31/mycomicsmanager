using Application.Statistics.Get;

namespace Application.Interfaces;

public interface IStatisticsReadService
{
    /// <summary>
    /// Computes the statistics of the user's books, optionally restricted to one library.
    /// <see cref="StatisticsDto.ReadingsPerMonth"/> only contains the months with at least one reading
    /// on or after <paramref name="readingsFromUtc"/>.
    /// </summary>
    Task<StatisticsDto> GetStatisticsAsync(
        Guid userId,
        Guid? libraryId,
        DateTime readingsFromUtc,
        CancellationToken cancellationToken = default);
}
