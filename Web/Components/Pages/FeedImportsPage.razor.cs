using Application.FeedImports.Arbitrate;
using Application.FeedImports.Manage;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Extensions;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages;

public partial class FeedImportsPage : IAsyncDisposable
{
    private const int PageSize = 20;

    [Inject] private IFeedImportService FeedImportService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private FeedImportNotifier Notifier { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ILogger<FeedImportsPage> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _disposeCts = new();
    private FeedImportFilter _filter = FeedImportFilter.All;
    private string? _searchTerm;
    private bool _showSearch;
    private bool _isSyncRequested;
    private Guid? _busyDecisionId;
    private CorrectionForm? _correction;
    private bool _isDeleting;

    // The decisions loaded so far (pages appended by "Show more"), in the order of the query.
    private readonly List<FeedImportDecisionViewModel> _items = [];
    private int _totalCount;
    private int _page;
    private bool _isLoading;
    private int _loadVersion;
    private FeedImportStatusCounts _counts = FeedImportStatusCounts.Empty;

    // The decision shown in detail: under its card on a phone, in the side panel from the tablet on.
    private Guid? _selectedId;

    // "Select several" (menu ⋯): the decisions checked for deletion.
    private bool _selecting;
    private readonly HashSet<Guid> _checkedIds = [];

    private bool HasMore => _items.Count < _totalCount;

    private FeedImportDecisionViewModel? SelectedDecision => _items.Find(d => d.Id == _selectedId);

    protected override Task OnInitializedAsync() => ReloadAsync();

    // Reloads the first page and the counts of the filters.
    private async Task ReloadAsync()
    {
        await Task.WhenAll(LoadPageAsync(1), LoadCountsAsync());
    }

    private Task LoadMoreAsync() => LoadPageAsync(_page + 1);

    internal async Task LoadPageAsync(int page)
    {
        // A first page replaces the list: any load still running for the previous filters becomes stale.
        var version = page == 1 ? ++_loadVersion : _loadVersion;
        var status = FeedImportFilters.StatusOf(_filter);
        var searchTerm = _searchTerm;

        _isLoading = true;
        try
        {
            var result = await FeedImportService.GetDecisionsAsync(status, searchTerm, page, PageSize, _disposeCts.Token);

            if (version != _loadVersion || status != FeedImportFilters.StatusOf(_filter) || searchTerm != _searchTerm)
            {
                return;
            }

            if (result.IsSuccess)
            {
                ApplyPage(page, result.Value!);
            }
            else if (result.IsFailure)
            {
                Snackbar.Add("Unable to load the import decisions.", Severity.Error);
                Logger.LogError("FeedImports: failed to load decisions: {ErrorDescription}", result.Error!.Description);
            }
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

    private void ApplyPage(int page, FeedImportDecisionPageViewModel decisions)
    {
        if (page == 1)
        {
            _items.Clear();
            _checkedIds.Clear();
        }

        // A decision created meanwhile shifts the pages: skip the ones already shown.
        var shown = _items.Select(d => d.Id).ToHashSet();
        _items.AddRange(decisions.Items.Where(d => !shown.Contains(d.Id)));
        _totalCount = decisions.TotalCount;
        _page = page;

        if (_selectedId is { } selectedId && !_items.Exists(d => d.Id == selectedId))
        {
            _selectedId = null;
        }
    }

    private async Task LoadCountsAsync()
    {
        var result = await FeedImportService.GetStatusCountsAsync(_disposeCts.Token);
        if (result.IsSuccess)
        {
            _counts = result.Value!;
        }
        else if (result.IsFailure)
        {
            Snackbar.Add("Unable to count the import decisions.", Severity.Error);
            Logger.LogError("FeedImports: failed to count decisions: {ErrorDescription}", result.Error!.Description);
        }
    }

    internal async Task SelectFilterAsync(FeedImportFilter filter)
    {
        if (filter == _filter)
        {
            return;
        }

        _filter = filter;
        await LoadPageAsync(1);
    }

    private async Task OnSearchChangedAsync(string? searchTerm)
    {
        _searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        await LoadPageAsync(1);
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

            await ReloadAsync();
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
                await ReloadAsync();
                Notifier.NotifyChanged();
                return true;
            }

            Snackbar.Add(result.Error?.Description ?? "Unable to update the decision.", Severity.Error);
            Logger.LogError("FeedImports: action {Action} on decision {DecisionId} failed: {ErrorDescription}",
                actionName, decisionId, result.Error?.Description);
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

            await ReloadAsync();
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
            "Delete downloaded",
            "Delete every downloaded decision whose import succeeded, with its history? The books already imported are kept; the decisions whose import is running or failed are kept too.",
            "Delete downloaded");
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

            await ReloadAsync();
            Notifier.NotifyChanged();
        }
        finally
        {
            _isDeleting = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
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
