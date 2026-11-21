using Application.Abstractions.Messaging;
using Application.Interfaces;
using Ardalis.GuardClauses;
using Domain.FeedImports;
using Domain.Primitives;

namespace Application.FeedImports.HostStatus;

public sealed class RecordHostStatusesCommandHandler(
    IDebridLinkClient debridLinkClient,
    IHostStatusChangeRepository changeRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<RecordHostStatusesCommand, RecordHostStatusesResult>
{
    public async Task<Result<RecordHostStatusesResult>> Handle(RecordHostStatusesCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);

        var hosts = await debridLinkClient.GetHostsAsync(cancellationToken);
        if (hosts.IsFailure)
        {
            return hosts.Error!;
        }

        var latest = await changeRepository.GetLatestPerHostAsync(cancellationToken);
        var changes = 0;
        var cameBackUp = new List<string>();
        foreach (var host in hosts.Value!.Where(h => !string.IsNullOrWhiteSpace(h.Name)).DistinctBy(h => h.Name.Trim(), StringComparer.Ordinal))
        {
            var known = latest.GetValueOrDefault(host.Name.Trim());
            if (known is not null && known.IsUp == host.IsUp)
            {
                continue;
            }

            var change = HostStatusChange.Create(host.Name, host.IsUp, timeProvider);
            if (change.IsFailure)
            {
                continue;
            }

            changeRepository.Add(change.Value!);
            changes++;
            if (known is { IsUp: false } && host.IsUp)
            {
                cameBackUp.Add(change.Value!.Host);
            }
        }

        if (changes > 0)
        {
            var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error!;
            }
        }

        var cutoff = timeProvider.GetUtcNow().UtcDateTime - FeedImportConstants.HostStatusRetention;
        var deleted = await changeRepository.DeleteOlderThanAsync(cutoff, cancellationToken);
        return new RecordHostStatusesResult(changes, cameBackUp, deleted);
    }
}
