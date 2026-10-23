using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Series;
using Application.Series.Get;
using Application.Series.List;
using Application.Series.Missing;
using Application.Series.SetTotal;
using Domain.Primitives;

namespace Web.Services;

public class SeriesService(
    IQueryHandler<GetLibrarySeriesQuery, IReadOnlyList<SerieSummary>> getLibrarySeriesHandler,
    IQueryHandler<GetSerieQuery, SerieDetail> getSerieHandler,
    IQueryHandler<GetMissingVolumesQuery, IReadOnlyList<LibraryMissingVolumes>> getMissingVolumesHandler,
    ICommandHandler<SetSerieTotalCommand> setSerieTotalHandler,
    ICurrentUserService currentUserService) : ISeriesService
{
    public async Task<Result<IReadOnlyList<SerieSummary>>> GetLibrarySeries(Guid libraryId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getLibrarySeriesHandler.Handle(new GetLibrarySeriesQuery(userIdResult.Value, libraryId), cancellationToken);
    }

    public async Task<Result<SerieDetail>> GetSerie(Guid libraryId, string name, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getSerieHandler.Handle(new GetSerieQuery(userIdResult.Value, libraryId, name), cancellationToken);
    }

    public async Task<Result<IReadOnlyList<LibraryMissingVolumes>>> GetMissingVolumes(CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        return await getMissingVolumesHandler.Handle(new GetMissingVolumesQuery(userIdResult.Value), cancellationToken);
    }

    public async Task<Result> SetTotal(Guid libraryId, string name, int? totalVolumes, bool isFinished, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var command = new SetSerieTotalCommand(userIdResult.Value, libraryId, name, totalVolumes, isFinished);
        return await setSerieTotalHandler.Handle(command, cancellationToken);
    }
}
