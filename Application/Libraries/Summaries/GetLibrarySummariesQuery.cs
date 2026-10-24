using Application.Abstractions.Messaging;

namespace Application.Libraries.Summaries;

// IncludeHidden lists the hidden libraries even when the user does not show them (e.g. the one being edited).
public sealed record GetLibrarySummariesQuery(Guid UserId, bool IncludeHidden = false) : IQuery<IReadOnlyList<LibrarySummary>>;
