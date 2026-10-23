using Domain.Series;

namespace Application.Series;

public interface ISeriesInfoRepository
{
    void Add(SeriesInfo seriesInfo);

    Task<SeriesInfo?> GetAsync(Guid libraryId, string normalizedName, CancellationToken cancellationToken = default);
}
