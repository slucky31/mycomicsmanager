using Application.Series;
using Application.Series.Get;
using Application.Series.Missing;
using Domain.Primitives;

namespace Web.Services;

public interface ISeriesService
{
    Task<Result<IReadOnlyList<SerieSummary>>> GetLibrarySeries(Guid libraryId, CancellationToken cancellationToken = default);

    Task<Result<SerieDetail>> GetSerie(Guid libraryId, string name, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LibraryMissingVolumes>>> GetMissingVolumes(CancellationToken cancellationToken = default);

    Task<Result> SetTotal(Guid libraryId, string name, int? totalVolumes, bool isFinished, CancellationToken cancellationToken = default);
}
