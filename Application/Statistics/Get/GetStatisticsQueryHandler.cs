using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.Libraries;
using Domain.Primitives;
using Domain.Statistics;

namespace Application.Statistics.Get;

public sealed class GetStatisticsQueryHandler(
    IStatisticsReadService statisticsReadService,
    IRepository<Library, Guid> libraryRepository,
    TimeProvider timeProvider) : IQueryHandler<GetStatisticsQuery, StatisticsDto>
{
    public const int MonthsInReadingHistory = 12;

    public async Task<Result<StatisticsDto>> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        if (request.UserId == Guid.Empty || request.LibraryId == Guid.Empty)
        {
            return StatisticsError.BadRequest;
        }

        if (request.LibraryId is { } libraryId)
        {
            var library = await libraryRepository.GetByIdAsync(libraryId);
            if (library is null || library.UserId != request.UserId)
            {
                return LibrariesError.NotFound;
            }
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(MonthsInReadingHistory - 1));

        var statistics = await statisticsReadService.GetStatisticsAsync(
            request.UserId,
            request.LibraryId,
            firstMonth,
            cancellationToken);

        return statistics with { ReadingsPerMonth = FillMissingMonths(statistics.ReadingsPerMonth, firstMonth) };
    }

    private static List<MonthlyReadingCountDto> FillMissingMonths(IReadOnlyList<MonthlyReadingCountDto> readings, DateTime firstMonth)
    {
        var countsByMonth = readings.ToDictionary(r => (r.Year, r.Month), r => r.Count);

        return Enumerable.Range(0, MonthsInReadingHistory)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month => new MonthlyReadingCountDto(
                month.Year,
                month.Month,
                countsByMonth.GetValueOrDefault((month.Year, month.Month))))
            .ToList();
    }
}
