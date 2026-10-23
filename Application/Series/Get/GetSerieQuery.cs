using Application.Abstractions.Messaging;

namespace Application.Series.Get;

public sealed record GetSerieQuery(Guid UserId, Guid LibraryId, string Name) : IQuery<SerieDetail>;
