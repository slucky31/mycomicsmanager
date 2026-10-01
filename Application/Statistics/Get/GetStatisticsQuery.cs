using Application.Abstractions.Messaging;

namespace Application.Statistics.Get;

public record GetStatisticsQuery(Guid UserId, Guid? LibraryId = null) : IQuery<StatisticsDto>;
