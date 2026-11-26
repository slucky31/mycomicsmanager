using Application.FeedImports.Arbitrate;
using Application.FeedImports.Manage;
using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Extensions;
using Web.Infrastructure;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages;

public partial class FeedImportsPage : IAsyncDisposable
{
    private const int PageSize = 20;
    private const string HostFilterButtonId = "feeds-host-filter";

    // Under the h2 of the side panel the sections are level 3; inline, the title of the card is a button, not a heading.
    private static int SectionLevel(bool inPanel) => inPanel ? 3 : 2;

    // The icon of a host in the host filter: green when up on Debrid-Link, red when down, plain when unknown.
    internal static string? HostStateColor(bool? isUp) => isUp switch
    {
        true => "var(--mcm-success)",
        false => "var(--mcm-error)",
        null => null,
    };

    [Inject] private IFeedImportService FeedImportService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private FeedImportNotifier Notifier { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ILogger<FeedImportsPage> Logger { get; set; } = default!;
    [Inject] private PopupAnchorLocator PopupAnchorLocator { get; set; } = default!;
    [Inject] private FeedImportsStateService StateService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly CancellationTokenSource _disposeCts = new();
    private FeedImportFilter _filter = FeedImportFilter.All;
    private string? _searchTerm;
    // The host of the links shown (FeedImportConstants.NoHost: the decisions without link); all of them when null.
    private string? _host;
    private FeedImportHostOptions _hostOptions = FeedImportHostOptions.Empty;
    private bool _isHostSheetOpen;
    private PopupAnchor? _hostSheetAnchor;
    private bool _showSearch;
    private bool _isSyncRequested;
    private Guid? _busyDecisionId;
    private CorrectionForm? _correction;
    private bool _isDeleting;
    private bool _isRetryingWaiting;
    // Failed decisions whose host is back up (or whose quota delay is over): the "Retry these" banner.
    private int _retryableCount;

    // The decisions loaded so far (pages appended by the infinite scroll), in the order of the query.
    private readonly List<FeedImportDecisionViewModel> _items = [];
    private int _totalCount;
    private int _page;
    private bool _isLoading;
    private int _loadVersion;
    private FeedImportStatusCounts _counts = FeedImportStatusCounts.Empty;

    // The decision shown in detail: under its card on a phone, in the side panel from the tablet on.
    private Guid? _selectedId;

    // The decision whose existing book was opened (#1386): its card comes back on screen on the way back.
    private Guid? _anchorId;

    // The card to bring back on screen once the feeds left are shown again.
    private Guid? _scrollToId;

    // "Select several" (menu ⋯): the decisions checked for deletion.
    private bool _selecting;
    private readonly HashSet<Guid> _checkedIds = [];

    private bool HasMore => _items.Count < _totalCount;

    private FeedImportDecisionViewModel? SelectedDecision => _items.Find(d => d.Id == _selectedId);

    internal static string CardId(Guid decisionId) => $"feed-{decisionId}";

    // Back from a book (#1386): the feeds open again with the filters, the pages and the detail they were left with.
    protected override async Task OnInitializedAsync()
    {
        if (StateService.Load() is { } saved)
        {
            _filter = saved.Filter;
            _host = saved.Host;
            _searchTerm = saved.SearchTerm;
            _selectedId = saved.SelectedId;
            _scrollToId = saved.AnchorId ?? saved.SelectedId;
            await LoadPagesAsync(1, Math.Max(saved.Pages, 1));
        }
        else
        {
            await LoadFirstPageAsync();
        }

        await LoadCountsAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_scrollToId is not { } id || _isLoading)
        {
            return;
        }

        if (!_items.Exists(d => d.Id == id))
        {
            _scrollToId = null;
            return;
        }

        try
        {
            var module = await JS.InvokeAsync<IJSObjectReference>("import", _disposeCts.Token, AppVersion.Versioned("./js/feedImports.js"));
            await module.InvokeVoidAsync("scrollToCard", _disposeCts.Token, CardId(id));
            await module.DisposeAsync();
            _scrollToId = null;
        }
        catch (OperationCanceledException)
        {
            // Page left while rendering.
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException)
        {
            _scrollToId = null;
            Logger.LogWarning(ex, "FeedImports: unable to bring the card {DecisionId} back on screen", id);
        }
    }

    // "View the existing book": the card of the decision is the one to bring back on screen.
    private void RememberAnchor(Guid decisionId) => _anchorId = decisionId;

    // After an action: the pages shown are loaded again, then the counts of the filters, so that the list keeps
    // its length and the scroll its position. One query after the other: the circuit's DbContext does not
    // support concurrent operations.
    private async Task RefreshAsync()
    {
        await LoadPagesAsync(1, Math.Max(_page, 1));
        await LoadCountsAsync();
    }

    // A decision changed in the meantime (download or import job): the list shows its real state again.
    private Task RefreshAfterConflictAsync(TError? error) =>
        error == PersistenceError.ConcurrencyConflict ? RefreshAsync() : Task.CompletedTask;

    private Task LoadFirstPageAsync() => LoadPagesAsync(1, 1);

    // Asked by the end of the list coming near the screen (infinite scroll).
    internal Task LoadMoreAsync() => _isLoading || !HasMore ? Task.CompletedTask : LoadPagesAsync(_page + 1, _page + 1);

    // From the first page, the pages replace the list; after it, they are added to it.
    private async Task LoadPagesAsync(int firstPage, int lastPage)
    {
        // A reload of the list makes any load still running for the previous filters stale.
        var version = firstPage == 1 ? ++_loadVersion : _loadVersion;
        var status = FeedImportFilters.StatusOf(_filter);
        var host = _host;
        var searchTerm = _searchTerm;
        var pages = new List<FeedImportDecisionPageViewModel>();

        _isLoading = true;
        try
        {
            for (var page = firstPage; page <= lastPage; page++)
            {
                var result = await FeedImportService.GetDecisionsAsync(status, host, searchTerm, page, PageSize, _disposeCts.Token);

                if (version != _loadVersion || status != FeedImportFilters.StatusOf(_filter) || host != _host || searchTerm != _searchTerm)
                {
                    return;
                }

                if (result.IsFailure)
                {
                    // The list shown is kept as it was.
                    Snackbar.Add("Unable to load the import decisions.", Severity.Error);
                    Logger.LogError("FeedImports: failed to load decisions: {ErrorDescription}", result.Error!.Description);
                    return;
                }

                pages.Add(result.Value!);
                if (page * PageSize >= result.Value!.TotalCount)
                {
                    break;
                }
            }

            ApplyPages(firstPage, pages);
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested)
        {
            // The page was left while loading.
        }
        finally
        {
            if (version == _loadVersion)
            {
                _isLoading = false;
            }
        }
    }

    private void ApplyPages(int firstPage, List<FeedImportDecisionPageViewModel> pages)
    {
        if (firstPage == 1)
        {
            _items.Clear();
        }

        // A decision created meanwhile shifts the pages: skip the ones already shown.
        var shown = _items.Select(d => d.Id).ToHashSet();
        foreach (var page in pages)
        {
            _items.AddRange(page.Items.Where(d => shown.Add(d.Id)));
        }

        _totalCount = pages[^1].TotalCount;
        _page = firstPage + pages.Count - 1;

        // Only the decisions still shown stay checked or selected.
        _checkedIds.IntersectWith(shown);
        if (_selectedId is { } selectedId && !shown.Contains(selectedId))
        {
            _selectedId = null;
        }
    }

    // The counts of the status chips follow the host chosen, those of the hosts the status chosen.
    private async Task LoadCountsAsync()
    {
        await LoadStatusCountsAsync();
        await LoadHostOptionsAsync();
        _retryableCount = await FeedImportService.CountRetryableAsync(_disposeCts.Token);
    }

    private async Task LoadStatusCountsAsync()
    {
        var result = await FeedImportService.GetStatusCountsAsync(_host, _disposeCts.Token);
        if (result.IsSuccess)
        {
            _counts = result.Value!;
        }
        else if (result.IsFailure)
        {
            ShowCountError(result.Error!);
        }
    }

    private async Task LoadHostOptionsAsync()
    {
        var result = await FeedImportService.GetHostOptionsAsync(FeedImportFilters.StatusOf(_filter), _disposeCts.Token);
        if (result.IsSuccess)
        {
            _hostOptions = result.Value!;
        }
        else if (result.IsFailure)
        {
            ShowCountError(result.Error!);
        }
    }

    private void ShowCountError(Domain.Primitives.TError error)
    {
        Snackbar.Add("Unable to count the import decisions.", Severity.Error);
        Logger.LogError("FeedImports: failed to count decisions: {ErrorDescription}", error.Description);
    }

    internal async Task SelectFilterAsync(FeedImportFilter filter)
    {
        if (filter == _filter)
        {
            return;
        }

        _filter = filter;
        await LoadFirstPageAsync();
        await LoadHostOptionsAsync();
    }

    // The menu opens under the button on a tablet, as a bottom sheet on a phone.
    internal async Task OpenHostSheetAsync()
    {
        _hostSheetAnchor = await PopupAnchorLocator.AnchorOfAsync(HostFilterButtonId, _disposeCts.Token);
        _isHostSheetOpen = true;
    }

    private void CloseHostSheet() => _isHostSheetOpen = false;

    internal async Task SelectHostAsync(string? host)
    {
        _isHostSheetOpen = false;
        if (host == _host)
        {
            return;
        }

        _host = host;
        await LoadFirstPageAsync();
        await LoadStatusCountsAsync();
    }

    private async Task OnSearchChangedAsync(string? searchTerm)
    {
        _searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        await LoadFirstPageAsync();
    }

    private void ToggleSearch() => _showSearch = !_showSearch;

    private void ToggleDetail(Guid id) => _selectedId = _selectedId == id ? null : id;

    internal void StartSelecting() => _selecting = true;

    private void StopSelecting()
    {
        _selecting = false;
        _checkedIds.Clear();
    }

    private void ToggleChecked(Guid id)
    {
        if (!_checkedIds.Remove(id))
        {
            _checkedIds.Add(id);
        }
    }

    internal async Task SyncNowAsync()
    {
        _isSyncRequested = true;
        try
        {
            var result = FeedImportService.TriggerSync();
            if (result.IsSuccess)
            {
                Snackbar.Add("Sync started. Refresh the list in a few moments.", Severity.Info);
            }
            else if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to start the sync.", Severity.Error);
                Logger.LogError("FeedImports: failed to trigger sync: {ErrorDescription}", result.Error!.Description);
            }

            await RefreshAsync();
        }
        finally
        {
            _isSyncRequested = false;
        }
    }

    private bool IsBusy(Guid decisionId) => _busyDecisionId == decisionId;

    internal Task ResolveAsync(Guid decisionId, FeedImportArbitrationAction action, int? candidateIndex) =>
        RunDecisionActionAsync(decisionId, action.ToString(),
            () => FeedImportService.ResolveArbitrationAsync(decisionId, action, candidateIndex, _disposeCts.Token));

    internal Task ApplyActionAsync(Guid decisionId, FeedImportDecisionAction action) =>
        RunDecisionActionAsync(decisionId, action.ToString(),
            () => FeedImportService.ApplyActionAsync(decisionId, action, _disposeCts.Token));

    private bool IsEditing(Guid decisionId) => _correction?.DecisionId == decisionId;

    internal void StartCorrection(FeedImportDecisionViewModel decision)
    {
        var actions = decision.ManualActions;
        _correction = new CorrectionForm(decision.Id) { Serie = actions.Serie ?? string.Empty, Title = actions.Title, Volume = actions.Volume };
    }

    private void CancelCorrection() => _correction = null;

    internal async Task SaveCorrectionAsync(Guid decisionId)
    {
        if (_correction is not { } correction || correction.DecisionId != decisionId)
        {
            return;
        }

        var succeeded = await RunDecisionActionAsync(decisionId, "Correct",
            () => FeedImportService.CorrectAsync(decisionId, correction.Serie, correction.Title, correction.Volume, _disposeCts.Token));
        if (succeeded)
        {
            _correction = null;
        }
    }

    private async Task<bool> RunDecisionActionAsync(Guid decisionId, string actionName, Func<Task<Domain.Primitives.Result>> action)
    {
        if (_busyDecisionId is not null)
        {
            return false;
        }

        _busyDecisionId = decisionId;
        try
        {
            var result = await action();
            if (result.IsSuccess)
            {
                Snackbar.Add("Decision updated.", Severity.Success);
                await RefreshAsync();
                Notifier.NotifyChanged();
                return true;
            }

            Snackbar.Add(result.Error?.Description ?? "Unable to update the decision.", Severity.Error);
            Logger.LogError("FeedImports: action {Action} on decision {DecisionId} failed: {ErrorDescription}",
                actionName, decisionId, result.Error?.Description);
            await RefreshAfterConflictAsync(result.Error);
            return false;
        }
        finally
        {
            _busyDecisionId = null;
        }
    }

    internal Task DeleteAsync(FeedImportDecisionViewModel decision) =>
        ConfirmAndDeleteAsync(
            [decision.Id],
            $"Delete the decision \"{decision.EntryTitle}\" and its history? The book already imported is kept.");

    internal Task DeleteSelectedAsync()
    {
        var ids = _items.Where(d => d.CanDelete && _checkedIds.Contains(d.Id)).Select(d => d.Id).ToList();
        var skipped = _checkedIds.Count - ids.Count;
        if (ids.Count == 0)
        {
            Snackbar.Add("The selected decisions are being downloaded.", Severity.Info);
            return Task.CompletedTask;
        }

        var message = $"Delete {ids.Count} decision(s) and their history? The books already imported are kept.";
        if (skipped > 0)
        {
            message += $" {skipped} decision(s) being downloaded will be kept.";
        }

        return ConfirmAndDeleteAsync(ids, message);
    }

    private async Task ConfirmAndDeleteAsync(List<Guid> decisionIds, string message)
    {
        if (decisionIds.Count == 0 || _isDeleting || _busyDecisionId is not null)
        {
            return;
        }

        var confirmed = await DialogService.ShowConfirmationAsync("Delete imports", message, "Delete imports");
        if (!confirmed)
        {
            return;
        }

        _isDeleting = true;
        try
        {
            var result = await FeedImportService.DeleteAsync(decisionIds, _disposeCts.Token);
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to delete the decisions.", Severity.Error);
                Logger.LogError("FeedImports: failed to delete decisions: {ErrorDescription}", result.Error!.Description);
                await RefreshAfterConflictAsync(result.Error);
                return;
            }

            if (decisionIds.Count > 1)
            {
                StopSelecting();
            }

            var notDeleted = decisionIds.Count - result.Value;
            if (notDeleted == 0)
            {
                Snackbar.Add($"{result.Value} decision(s) deleted.", Severity.Success);
            }
            else
            {
                Snackbar.Add($"{result.Value} decision(s) deleted, {notDeleted} not deleted (download or import in progress).", Severity.Warning);
            }

            await RefreshAsync();
            Notifier.NotifyChanged();
        }
        finally
        {
            _isDeleting = false;
        }
    }

    internal async Task DeleteDownloadedAsync()
    {
        if (_isDeleting || _busyDecisionId is not null)
        {
            return;
        }

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Clear successful items",
            "Delete every downloaded item whose import succeeded, with its history? The books already imported are kept; the items whose import is running or failed are kept too.",
            "Clear");
        if (!confirmed)
        {
            return;
        }

        _isDeleting = true;
        try
        {
            var result = await FeedImportService.DeleteDownloadedAsync(_disposeCts.Token);
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to delete the downloaded decisions.", Severity.Error);
                Logger.LogError("FeedImports: failed to delete downloaded decisions: {ErrorDescription}", result.Error!.Description);
                await RefreshAfterConflictAsync(result.Error);
                return;
            }

            var summary = result.Value!;
            if (summary.Kept == 0)
            {
                Snackbar.Add($"{summary.Deleted} decision(s) deleted.", Severity.Success);
            }
            else
            {
                Snackbar.Add($"{summary.Deleted} decision(s) deleted, {summary.Kept} kept (import running or failed).", Severity.Info);
            }

            await RefreshAsync();
            Notifier.NotifyChanged();
        }
        finally
        {
            _isDeleting = false;
        }
    }

    internal async Task RetryWaitingAsync()
    {
        if (_isRetryingWaiting || _retryableCount == 0)
        {
            return;
        }

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Retry failed items",
            _retryableCount == 1
                ? "Retry the failed item whose host is back up on Debrid-Link (or whose quota delay is over)?"
                : $"Retry the {_retryableCount} failed items whose host is back up on Debrid-Link (or whose quota delay is over)?",
            "Retry");
        if (!confirmed)
        {
            return;
        }

        _isRetryingWaiting = true;
        try
        {
            var result = await FeedImportService.RetryWaitingAsync(_disposeCts.Token);
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to retry the failed items.", Severity.Error);
                Logger.LogError("FeedImports: failed to retry the waiting decisions: {ErrorDescription}", result.Error!.Description);
                await RefreshAfterConflictAsync(result.Error);
                return;
            }

            Snackbar.Add($"{result.Value} item(s) retried.", Severity.Success);
            await RefreshAsync();
            Notifier.NotifyChanged();
        }
        finally
        {
            _isRetryingWaiting = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        StateService.Save(new FeedImportsPageState(_filter, _host, _searchTerm, _page, _selectedId, _anchorId));
        await _disposeCts.CancelAsync();
        _disposeCts.Dispose();
    }

    private sealed class CorrectionForm(Guid decisionId)
    {
        public Guid DecisionId { get; } = decisionId;
        public string Serie { get; set; } = string.Empty;
        public string? Title { get; set; }
        public int? Volume { get; set; }
    }
}
