using Application.Series;
using Domain.Series;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class SeriesInfoRepository(ApplicationDbContext context) : ISeriesInfoRepository
{
    public void Add(SeriesInfo seriesInfo) => context.SeriesInfos.Add(seriesInfo);

    public async Task<SeriesInfo?> GetAsync(Guid libraryId, string normalizedName, CancellationToken cancellationToken = default)
        => await context.SeriesInfos.FirstOrDefaultAsync(s => s.LibraryId == libraryId && s.NormalizedName == normalizedName, cancellationToken);
}
