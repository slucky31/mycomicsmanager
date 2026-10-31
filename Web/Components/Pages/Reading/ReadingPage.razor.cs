using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Reading;

// The books in progress and the last readings (#1252), each with the actions of the book (#1265).
public partial class ReadingPage : IAsyncDisposable
{
    internal const int ReadingsPageSize = 20;

    [Inject] private IReadingService ReadingService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<ReadingPage> Logger { get; set; } = default!;
    [Inject] private PopupAnchorLocator PopupAnchorLocator { get; set; } = default!;

    private IReadOnlyList<BookInProgressViewModel> _inProgress = [];
    private List<BookReadingViewModel> _readings = [];
    private IReadOnlyList<ReadingMonthViewModel> _months = [];
    private int ReadingCount { get; set; }
    private bool _isLoading;
    private bool _isLoadingMore;
    private bool _loaded;
    private bool _loadFailed;
    private readonly CancellationTokenSource _disposalCts = new();

    private BookActionsTarget? _actionsBook;
    private bool _actionsDigital;
    private bool _isActionsOpen;
    private PopupAnchor? _actionsAnchor;

    private int RemainingReadings => Math.Max(0, ReadingCount - _readings.Count);

    protected override Task OnInitializedAsync() => LoadAsync();

    // One query after the other: the circuit shares one DbContext.
    private async Task LoadAsync()
    {
        _isLoading = true;
        _loadFailed = false;
        try
        {
            var inProgress = await ReadingService.GetInProgress(_disposalCts.Token);
            if (inProgress.IsFailure)
            {
                ShowLoadError(inProgress.Error);
                return;
            }

            var readings = await ReadingService.GetRecentReadings(1, ReadingsPageSize, _disposalCts.Token);
            if (readings.IsFailure)
            {
                ShowLoadError(readings.Error);
                return;
            }

            _inProgress = [.. inProgress.Value!.Select(BookInProgressViewModel.From)];
            _readings = [.. (readings.Value!.Items ?? []).Select(BookReadingViewModel.From)];
            ReadingCount = readings.Value.TotalCount;
            _months = ReadingPageDisplay.ByMonth(_readings);
            _loaded = true;
        }
        catch (OperationCanceledException) when (_disposalCts.IsCancellationRequested)
        {
            // The page was left.
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadMoreAsync()
    {
        if (_isLoadingMore)
        {
            return;
        }

        // Every page before the last one is full.
        var loaded = _readings.Count;
        var page = loaded / ReadingsPageSize + 1;
        _isLoadingMore = true;
        try
        {
            var result = await ReadingService.GetRecentReadings(page, ReadingsPageSize, _disposalCts.Token);
            if (loaded != _readings.Count)
            {
                return;
            }

            if (result.IsSuccess)
            {
                _readings.AddRange((result.Value!.Items ?? []).Select(BookReadingViewModel.From));
                ReadingCount = result.Value.TotalCount;
                _months = ReadingPageDisplay.ByMonth(_readings);
            }
            else if (result.IsFailure)
            {
                Snackbar.Add("Failed to load more readings", Severity.Error);
                Logger.LogError("Reading: failed to load the readings of page {Page}: {Error}", page, result.Error?.Code);
            }
        }
        catch (OperationCanceledException) when (_disposalCts.IsCancellationRequested)
        {
            // The page was left.
        }
        finally
        {
            _isLoadingMore = false;
        }
    }

    // The menu opens next to the ⋮ on a tablet, as a bottom sheet on a phone.
    internal async Task OpenActionsAsync(BookActionsTarget book, bool isDigital, string buttonId)
    {
        _actionsAnchor = await PopupAnchorLocator.AnchorOfAsync(buttonId, _disposalCts.Token);
        _actionsBook = book;
        _actionsDigital = isDigital;
        _isActionsOpen = true;
    }

    private void CloseActions() => _isActionsOpen = false;

    private void ShowLoadError(TError? error)
    {
        // A failed reload keeps nothing of a previous load: the page offers to try again.
        _loaded = false;
        _loadFailed = true;
        Snackbar.Add("Failed to load your readings", Severity.Error);
        Logger.LogError("Reading: failed to load the readings: {Error}", error?.Code);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
    }
}
