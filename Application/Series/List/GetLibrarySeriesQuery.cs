using Application.Abstractions.Messaging;

namespace Application.Series.List;

public sealed record GetLibrarySeriesQuery(Guid UserId, Guid LibraryId) : IQuery<IReadOnlyList<SerieSummary>>;
