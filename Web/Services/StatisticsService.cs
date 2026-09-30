using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Statistics.Get;
using Domain.Primitives;

namespace Web.Services;

public class StatisticsService(
    IQueryHandler<GetStatisticsQuery, StatisticsDto> getStatisticsHandler,
    ICurrentUserService currentUserService) : IStatisticsService
{
    public async Task<Result<StatisticsDto>> Get(Guid? libraryId, CancellationToken cancellationToken = default)
    {
        var userIdResult = await currentUserService.GetCurrentUserIdAsync(cancellationToken);
        if (userIdResult.IsFailure)
        {
            return userIdResult.Error!;
        }

        var query = new GetStatisticsQuery(userIdResult.Value, libraryId);

        return await getStatisticsHandler.Handle(query, cancellationToken);
    }
}
