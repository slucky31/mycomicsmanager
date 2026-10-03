using Application.Abstractions.Messaging;
using Application.ImportJobs.Create;
using Application.Interfaces;
using Application.Libraries.Create;
using Ardalis.GuardClauses;
using Domain.Extensions;
using Domain.FeedImports;
using Domain.Libraries;
using Domain.Primitives;

namespace Application.FeedImports.Download;

// LinksExtracted decision -> unlock a mirror with Debrid-Link -> stream the file into the temp directory
// -> ImportJob created -> file deposited in the import directory of the target library -> Downloaded.
// The ImportJob is created before the file reaches the watched directory, so the FileWatcher finds an
// active job for that path and never creates a second one.
public sealed class DownloadFeedImportDecisionCommandHandler(
    IFeedImportDecisionRepository decisionRepository,
    IUnitOfWork unitOfWork,
    FeedImportDownloadServices downloadServices,
    FeedImportDepositServices depositServices,
    FeedImportDownloadOptions options) : ICommandHandler<DownloadFeedImportDecisionCommand>
{
    public const string LibraryStep = "Bibliothèque";
    public const string DownloadStep = "Téléchargement";
    public const string ImportStep = "Import";

    public const string TargetLibraryColor = "#5C6BC0";
    public const string TargetLibraryIcon = "CollectionsBookmark";

    private static Serilog.ILogger Log => Serilog.Log.ForContext<DownloadFeedImportDecisionCommandHandler>();

    // These failures would be the same on every mirror: trying the next one would only burn the quota.
    private static readonly HashSet<string> s_stopCodes =
    [
        FeedImportError.DebridLinkUnauthorized.Code,
        FeedImportError.DebridLinkQuotaReached.Code,
        FeedImportError.FileTooLarge.Code,
        FeedImportError.UnsupportedFileType.Code
    ];

    public async Task<Result> Handle(DownloadFeedImportDecisionCommand command, CancellationToken cancellationToken)
    {
        Guard.Against.Null(command);
        if (command.DecisionId == Guid.Empty)
        {
            return FeedImportError.BadRequest;
        }

        if (string.IsNullOrWhiteSpace(options.DebridLink.Value.ApiKey))
        {
            return FeedImportError.DebridLinkUnauthorized;
        }

        var decision = await decisionRepository.GetByIdAsync(command.DecisionId, cancellationToken);
        if (decision is null)
        {
            return FeedImportError.NotFound;
        }

        // Already downloaded, waiting for the user, ignored...: nothing to do.
        if (decision.Status != FeedImportDecisionStatus.LinksExtracted)
        {
            return Result.Success();
        }

        var candidates = decision.GetCandidates();
        var candidate = candidates.Count > 0 ? candidates[0] : null;
        if (candidate is null || candidate.Mirrors.Count == 0)
        {
            return await FailAsync(decision, DownloadStep, "Aucun lien de téléchargement enregistré.", cancellationToken);
        }

        var library = await GetOrCreateTargetLibraryAsync(decision.UserId, cancellationToken);
        if (library.IsFailure)
        {
            return await FailAsync(decision, LibraryStep, library.Error!.Description ?? library.Error.Code, cancellationToken);
        }

        var started = decision.StartDownload();
        if (started.IsFailure)
        {
            return started;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error!;
        }

        var download = await DownloadFromMirrorsAsync(decision, candidate, cancellationToken);
        if (download.IsFailure)
        {
            return await FailAsync(decision, DownloadStep, download.Error!.Description ?? download.Error.Code, cancellationToken);
        }

        return await DepositAsync(decision, library.Value!, download.Value!, cancellationToken);
    }

    private async Task<Result<Library>> GetOrCreateTargetLibraryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var name = options.FeedImport.Value.TargetLibraryName;
        var library = await depositServices.LibraryReadService.GetByNameAsync(name, userId, cancellationToken);
        if (library is null)
        {
            var created = await depositServices.CreateLibraryHandler.Handle(
                new CreateLibraryCommand(name, TargetLibraryColor, TargetLibraryIcon, LibraryBookType.Digital, userId), cancellationToken);
            if (created.IsFailure)
            {
                return created.Error!;
            }

            Log.Information("Feed import: digital library {LibraryName} created for the downloads", name);
            return created.Value!;
        }

        if (library.BookType != LibraryBookType.Digital)
        {
            return FeedImportError.TargetLibraryInvalid;
        }

        // Created before the import directories existed, or deleted by hand.
        var ensured = depositServices.ImportDirectoryStorage.EnsureExists(library.ImportDirectoryName);
        return ensured.IsFailure ? ensured.Error! : library;
    }

    private async Task<Result<MirrorDownload>> DownloadFromMirrorsAsync(
        FeedImportDecision decision, DownloadCandidate candidate, CancellationToken cancellationToken)
    {
        var mirrors = await GetUsableMirrorsAsync(decision, candidate.Mirrors, cancellationToken);
        if (mirrors.Count == 0)
        {
            return FeedImportError.DebridLinkHostNotSupported;
        }

        TError lastError = FeedImportError.DownloadFailed;
        foreach (var mirror in mirrors)
        {
            var result = await DownloadMirrorAsync(mirror, candidate, cancellationToken);
            if (result.IsSuccess)
            {
                return result;
            }

            lastError = result.Error!;
            Log.Warning("Feed import: mirror {Host} of decision {DecisionId} failed: [{Code}] {Description}",
                mirror.Host, decision.Id, lastError.Code, lastError.Description);
            if (s_stopCodes.Contains(lastError.Code))
            {
                return lastError;
            }

            decision.NoteMirrorFailure(mirror.Host, lastError.Description ?? lastError.Code);
        }

        return lastError;
    }

    // Mirrors Debrid-Link can unlock, by FeedImport priority. If the domain list is unavailable, all are tried.
    private async Task<IReadOnlyList<DownloadMirror>> GetUsableMirrorsAsync(
        FeedImportDecision decision, IReadOnlyList<DownloadMirror> mirrors, CancellationToken cancellationToken)
    {
        var ordered = OrderByPriority(mirrors, options.DebridLink.Value.HosterPriority);
        var supported = await downloadServices.DebridLinkClient.GetSupportedDomainsAsync(cancellationToken);
        if (supported.IsFailure)
        {
            return ordered;
        }

        var usable = ordered.Where(m => m.Host.IsSameOrSubdomainOf(supported.Value!)).ToList();
        foreach (var skipped in ordered.Except(usable))
        {
            decision.NoteMirrorFailure(skipped.Host, FeedImportError.DebridLinkHostNotSupported.Description!);
        }

        return usable;
    }

    public static IReadOnlyList<DownloadMirror> OrderByPriority(IReadOnlyList<DownloadMirror> mirrors, IReadOnlyList<string> hosterPriority)
    {
        Guard.Against.Null(mirrors);
        Guard.Against.Null(hosterPriority);

        int Rank(DownloadMirror mirror)
        {
            for (var i = 0; i < hosterPriority.Count; i++)
            {
                if (mirror.Host.IsSameOrSubdomainOf([hosterPriority[i]]))
                {
                    return i;
                }
            }

            return hosterPriority.Count;
        }

        // OrderBy is stable: mirrors with the same rank keep the page order.
        return mirrors.OrderBy(Rank).ToList();
    }

    private async Task<Result<MirrorDownload>> DownloadMirrorAsync(
        DownloadMirror mirror, DownloadCandidate candidate, CancellationToken cancellationToken)
    {
        var unlocked = await downloadServices.DebridLinkClient.UnlockAsync(mirror.Url, cancellationToken);
        if (unlocked.IsFailure)
        {
            return unlocked.Error!;
        }

        var file = unlocked.Value!;
        var fileName = Path.GetFileName(string.IsNullOrWhiteSpace(file.Name) ? candidate.FileName ?? string.Empty : file.Name);
        var importSettings = options.Import.Value;
        if (!importSettings.SupportedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
        {
            return new TError(FeedImportError.UnsupportedFileType.Code, $"{FeedImportError.UnsupportedFileType.Description} ({fileName})");
        }

        var maxBytes = importSettings.MaxFileSizeMb * 1024L * 1024L;
        if (file.SizeBytes > maxBytes)
        {
            return FeedImportError.FileTooLarge;
        }

        if (!IsAllowedDownloadUrl(file.DownloadUrl))
        {
            return new TError(FeedImportError.DownloadHostNotAllowed.Code, $"{FeedImportError.DownloadHostNotAllowed.Description} ({file.DownloadUrl.Host})");
        }

        var downloaded = await downloadServices.FileDownloader.DownloadAsync(file.DownloadUrl, maxBytes, cancellationToken);
        if (downloaded.IsFailure)
        {
            return downloaded.Error!;
        }

        return new MirrorDownload(mirror, fileName, downloaded.Value!);
    }

    private bool IsAllowedDownloadUrl(Uri downloadUrl)
    {
        var hosts = options.FeedImport.Value.AllowedDownloadHosts.Concat(options.DebridLink.Value.DownloadHosts);
        return downloadUrl.IsAbsoluteUri
               && downloadUrl.Scheme == Uri.UriSchemeHttps
               && downloadUrl.Host.IsSameOrSubdomainOf(hosts);
    }

    private async Task<Result> DepositAsync(
        FeedImportDecision decision, Library library, MirrorDownload download, CancellationToken cancellationToken)
    {
        var storage = depositServices.ImportDirectoryStorage;
        var path = storage.GetAvailableFilePath(library.ImportDirectoryName, download.FileName);
        if (path.IsFailure)
        {
            return await DiscardAndFailAsync(decision, download, path.Error!, cancellationToken);
        }

        var job = await depositServices.CreateImportJobHandler.Handle(
            new CreateImportJobCommand(Path.GetFileName(path.Value!), path.Value!, download.File.SizeBytes, library.Id, decision.UserId),
            cancellationToken);
        if (job.IsFailure)
        {
            return await DiscardAndFailAsync(decision, download, job.Error!, cancellationToken);
        }

        var deposited = storage.DepositFile(download.File.TempFilePath, path.Value!);
        if (deposited.IsFailure)
        {
            job.Value!.Fail(ImportStep, deposited.Error!.Description ?? deposited.Error.Code);
            return await DiscardAndFailAsync(decision, download, deposited.Error, cancellationToken);
        }

        var marked = decision.MarkDownloaded(download.Mirror.Url, job.Value!.Id, library.Name);
        if (marked.IsFailure)
        {
            return marked;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error!;
        }

        depositServices.ImportJobEnqueuer.Enqueue(job.Value.Id);
        Log.Information("Feed import: decision {DecisionId} downloaded from {Host} into {Path}, import job {ImportJobId}",
            decision.Id, download.Mirror.Host, path.Value, job.Value.Id);
        return Result.Success();
    }

    private async Task<Result> DiscardAndFailAsync(
        FeedImportDecision decision, MirrorDownload download, TError error, CancellationToken cancellationToken)
    {
        downloadServices.FileDownloader.Discard(download.File.TempFilePath);
        return await FailAsync(decision, ImportStep, error.Description ?? error.Code, cancellationToken);
    }

    // The failure is recorded on the decision: the command itself succeeded.
    private async Task<Result> FailAsync(FeedImportDecision decision, string step, string message, CancellationToken cancellationToken)
    {
        var failed = decision.Fail(step, message);
        if (failed.IsFailure)
        {
            return failed;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error! : Result.Success();
    }

    private sealed record MirrorDownload(DownloadMirror Mirror, string FileName, DownloadedFile File);
}
