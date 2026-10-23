using Application.Abstractions.Messaging;
using Application.Interfaces;
using Application.Libraries;
using Ardalis.GuardClauses;
using Domain.Primitives;

namespace Application.ImportJobs.MigrateLegacy;

// The old library is on its own volume: each move is a copy of the whole archive, minutes for a batch, so the files
// are moved by a background job and the Settings page is answered at once (#1204).
public sealed class StartLegacyMigrationCommandHandler(
    ILegacyLibrarySource legacyLibrarySource,
    ILibraryReadService libraryReadService,
    ILegacyMigrationJobEnqueuer enqueuer) : ICommandHandler<StartLegacyMigrationCommand, int>
{
    public async Task<Result<int>> Handle(StartLegacyMigrationCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        var plan = await LegacyMigrationPlan.CreateAsync(request.Count, request.UserId, libraryReadService, legacyLibrarySource, cancellationToken);
        if (plan.IsFailure)
        {
            return plan.Error!;
        }

        enqueuer.Enqueue(request.Count, request.UserId);
        return plan.Value!.Batch.Files.Count;
    }
}
