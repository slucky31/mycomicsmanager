using System.Globalization;
using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries;
using Application.Users;
using Domain.Primitives;
using Domain.Series;
using Domain.Users;

namespace Application.Series.Missing;

// The volumes missing in the series of every library of the user, by library name (#1205),
// without the hidden libraries unless the user shows them (#1215).
public sealed class GetMissingVolumesQueryHandler(
    ISeriesReadService seriesReadService,
    ILibraryReadService libraryReadService,
    IRepository<User, Guid> userRepository)
    : IQueryHandler<GetMissingVolumesQuery, IReadOnlyList<LibraryMissingVolumes>>
{
    private static readonly StringComparer s_nameComparer = StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase: true);

    public async Task<Result<IReadOnlyList<LibraryMissingVolumes>>> Handle(GetMissingVolumesQuery request, CancellationToken cancellationToken)
    {
        if (request is null || request.UserId == Guid.Empty)
        {
            return SeriesError.BadRequest;
        }

        IReadOnlySet<Guid> hidden = await userRepository.ShowsHiddenLibrariesAsync(request.UserId)
            ? new HashSet<Guid>()
            : await libraryReadService.ListHiddenIdsAsync(request.UserId, cancellationToken);
        var books = (await seriesReadService.ListBooksAsync(request.UserId, null, cancellationToken))
            .Where(b => !hidden.Contains(b.LibraryId))
            .ToList();
        var infos = (await seriesReadService.ListInfosAsync(request.UserId, null, cancellationToken))
            .Where(i => !hidden.Contains(i.LibraryId))
            .ToList();

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
