using Domain.Primitives;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Reading;

// The books in progress and the last readings (#1252).
public partial class ReadingPage : IAsyncDisposable
{
    internal const int ReadingsPageSize = 20;

    [Inject] private IReadingService ReadingService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<ReadingPage> Logger { get; set; } = default!;

    private IReadOnlyList<BookInProgressViewModel> _inProgress = [];
    private List<BookReadingViewModel> _readings = [];
    private IReadOnlyList<ReadingMonthViewModel> _months = [];
    private int _readingCount;
    private int _readingsPage;
    private bool _isLoading;
    private bool _isLoadingMore;
    private bool _loaded;
    private bool _loadFailed;
    private readonly CancellationTokenSource _disposalCts = new();

    private int RemainingReadings => Math.Max(0, _readingCount - _readings.Count);

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
            _readingCount = readings.Value.TotalCount;
            _readingsPage = 1;
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

        var page = _readingsPage + 1;
        _isLoadingMore = true;
        try
        {
            var result = await ReadingService.GetRecentReadings(page, ReadingsPageSize, _disposalCts.Token);
            if (page != _readingsPage + 1)
            {
                return;
            }

            if (result.IsSuccess)
            {
                _readings.AddRange((result.Value!.Items ?? []).Select(BookReadingViewModel.From));
                _readingCount = result.Value.TotalCount;
                _readingsPage = page;
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
