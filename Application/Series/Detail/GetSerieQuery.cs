using Application.Abstractions.Messaging;

namespace Application.Series.Detail;

public sealed record GetSerieQuery(Guid UserId, Guid LibraryId, string Name) : IQuery<SerieDetail>;
