using Application.Abstractions.Messaging;

namespace Application.FeedImports.Arbitrate;

public record ResolveFeedImportArbitrationCommand(
    Guid DecisionId,
    Guid UserId,
    FeedImportArbitrationAction Action,
    int? CandidateIndex = null) : ICommand;
