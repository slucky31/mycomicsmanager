using Application.Abstractions.Messaging;

namespace Application.Statistics.GetStatistics;

public record GetStatisticsQuery(Guid UserId, Guid? LibraryId = null) : IQuery<StatisticsDto>;
