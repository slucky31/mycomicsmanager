using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Application.FeedImports.Sync;

public sealed class SyncFeedImportsCommandHandler(
    IMinifluxClient minifluxClient,
    IFeedImportDecisionRepository decisionRepository,
    IUnitOfWork unitOfWork,
    IOptions<MinifluxSettings> minifluxSettings) : ICommandHandler<SyncFeedImportsCommand, SyncFeedImportsResult>
{
    private static Serilog.ILogger Log => Serilog.Log.ForContext<SyncFeedImportsCommandHandler>();

    private enum EntryOutcome
    {
        Created,
        AlreadyKnown,
        Rejected
    }

    public async Task<Result<SyncFeedImportsResult>> Handle(SyncFeedImportsCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);

        var categoryName = minifluxSettings.Value.CategoryName?.Trim();
        if (command.UserId == Guid.Empty || string.IsNullOrEmpty(categoryName))
        {
            return FeedImportError.BadRequest;
        }

        var categoriesResult = await minifluxClient.GetCategoriesAsync(cancellationToken);
        if (categoriesResult.IsFailure)
        {
            return categoriesResult.Error!;
        }

        var category = categoriesResult.Value!
            .FirstOrDefault(c => string.Equals(c.Title.Trim(), categoryName, StringComparison.OrdinalIgnoreCase));
        if (category is null)
        {
            return FeedImportError.CategoryNotFound;
        }

        var entriesResult = await minifluxClient.GetStarredEntriesAsync(category.Id, cancellationToken);
        if (entriesResult.IsFailure)
        {
            return entriesResult.Error!;
        }

        var entries = entriesResult.Value!;
        int created = 0, alreadyKnown = 0, rejected = 0, unstarFailures = 0;
        foreach (var entry in entries)
        {
            var (outcome, unstarred) = await ProcessEntryAsync(command.UserId, entry, cancellationToken);
            switch (outcome)
            {
                case EntryOutcome.Created:
                    created++;
                    break;
                case EntryOutcome.AlreadyKnown:
                    alreadyKnown++;
                    break;
                default:
                    rejected++;
                    break;
            }

            if (outcome != EntryOutcome.Rejected && !unstarred)
            {
                unstarFailures++;
            }
        }

        return new SyncFeedImportsResult(entries.Count, created, alreadyKnown, rejected, unstarFailures);
    }

    // The decision is always persisted before the star is removed: if unstarring fails, the next sync
    // finds the entry already known (unique MinifluxEntryId) and only retries the unstar.
    private async Task<(EntryOutcome Outcome, bool Unstarred)> ProcessEntryAsync(Guid userId, MinifluxEntry entry, CancellationToken cancellationToken)
    {
        var outcome = EntryOutcome.AlreadyKnown;
        var existing = await decisionRepository.GetByMinifluxEntryIdAsync(userId, entry.Id, cancellationToken);
        if (existing is null)
        {
            if (!await TrySaveDecisionAsync(userId, entry, cancellationToken))
            {
                // Left starred on purpose: it is picked up again by the next sync.
                return (EntryOutcome.Rejected, false);
            }
            outcome = EntryOutcome.Created;
        }

        var unstarResult = await minifluxClient.UnstarAsync(entry.Id, cancellationToken);
        if (unstarResult.IsFailure)
        {
            Log.Warning("Feed import: failed to unstar Miniflux entry {EntryId}: [{Code}] {Description}",
                entry.Id, unstarResult.Error!.Code, unstarResult.Error.Description);
        }

        return (outcome, unstarResult.IsSuccess);
    }

    private async Task<bool> TrySaveDecisionAsync(Guid userId, MinifluxEntry entry, CancellationToken cancellationToken)
    {
        var createResult = FeedImportDecision.Create(userId, entry.Id, entry.Title, entry.Url, entry.PublishedAt?.UtcDateTime);
        if (createResult.IsFailure)
        {
            Log.Warning("Feed import: Miniflux entry {EntryId} rejected ({Url}): {Description}",
                entry.Id, entry.Url, createResult.Error!.Description);
            return false;
        }

        var decision = createResult.Value!;
        decisionRepository.Add(decision);
        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            // Detach the unsaved decision so it is not re-sent with the next entry's SaveChanges.
            decisionRepository.Remove(decision);
            Log.Error("Feed import: failed to save decision for Miniflux entry {EntryId}: [{Code}] {Description}",
                entry.Id, saveResult.Error!.Code, saveResult.Error.Description);
            return false;
        }

        return true;
    }
}
