namespace Web.Models;

public sealed record FeedImportDecisionPageViewModel(IReadOnlyList<FeedImportDecisionViewModel> Items, int TotalCount);
