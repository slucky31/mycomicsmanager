using Application.Statistics.Get;

namespace Application.Interfaces;

public interface IStatisticsReadService
{
    /// <summary>
    /// Computes the statistics of the user's books, optionally restricted to one library.
    /// <see cref="StatisticsDto.ReadingsPerMonth"/> only contains the months with at least one reading
    /// on or after <paramref name="readingsFromUtc"/>.
    /// Without a library, <paramref name="includeHidden"/> false leaves out the hidden libraries.
    /// </summary>
    Task<StatisticsDto> GetStatisticsAsync(
        Guid userId,
        Guid? libraryId,
        bool includeHidden,
        DateTime readingsFromUtc,
        CancellationToken cancellationToken = default);
}
