using System.Globalization;
using Application.Abstractions.Messaging;
using Domain.Primitives;
using Domain.Series;

namespace Application.Series.Missing;

// The volumes missing in the series of every library of the user, by library name (#1205).
public sealed class GetMissingVolumesQueryHandler(ISeriesReadService seriesReadService)
    : IQueryHandler<GetMissingVolumesQuery, IReadOnlyList<LibraryMissingVolumes>>
{
    private static readonly StringComparer s_nameComparer = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);

    public async Task<Result<IReadOnlyList<LibraryMissingVolumes>>> Handle(GetMissingVolumesQuery request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return SeriesError.BadRequest;
        }

        var books = await seriesReadService.ListBooksAsync(request.UserId, null, cancellationToken);
        var infos = await seriesReadService.ListInfosAsync(request.UserId, null, cancellationToken);

        var libraryNames = books
            .GroupBy(b => b.LibraryId)
            .ToDictionary(g => g.Key, g => g.First().LibraryName);

        IReadOnlyList<LibraryMissingVolumes> libraries = SerieGrouping.Summarize(books, infos)
            .Where(s => s.Completeness.Status == SerieCompletenessStatus.Incomplete)
            .GroupBy(s => s.LibraryId)
            .Select(g => new LibraryMissingVolumes(g.Key, libraryNames[g.Key], g.ToList()))
            .OrderBy(l => l.LibraryName, s_nameComparer)
            .ThenBy(l => l.LibraryId)
            .ToList();

        return Result<IReadOnlyList<LibraryMissingVolumes>>.Success(libraries);
    }
}
