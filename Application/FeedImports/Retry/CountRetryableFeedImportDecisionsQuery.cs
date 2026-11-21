using Application.Abstractions.Messaging;

namespace Application.FeedImports.Retry;

// The failed decisions the "Retry these" button would retry now.
public record CountRetryableFeedImportDecisionsQuery(Guid UserId) : IQuery<int>;
