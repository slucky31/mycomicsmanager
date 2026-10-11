using Application.Abstractions.Messaging;

namespace Application.Libraries.Summaries;

public sealed record GetLibrarySummariesQuery(Guid UserId) : IQuery<IReadOnlyList<LibrarySummary>>;
