using Domain.FeedImports;

namespace Application.FeedImports.Analysis;

public sealed record LinkGroupingResult(IReadOnlyList<DownloadCandidate> Candidates, bool IsAmbiguous, string? AmbiguityReason);
