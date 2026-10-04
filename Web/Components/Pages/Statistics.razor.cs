using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages;

public partial class Statistics : IAsyncDisposable
{
    private const int MaxLibraries = 200;

    [Inject] private IStatisticsService StatisticsService { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<Statistics> Logger { get; set; } = default!;

    private readonly BarChartOptions _chartOptions = new()
    {
        ShowLegend = false,
        YAxisFormat = "0",
        MaxNumYAxisTicks = 5,
        XAxisLabelRotation = 45
    };

    private List<LibraryUiDto> _libraries = [];
    private Guid _selectedLibraryId = Guid.Empty;
    private StatisticsViewModel? _statistics;
    private string[] _monthLabels = [];
    private bool _isLoading;
    private CancellationTokenSource _loadCts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadLibrariesAsync();
        await LoadStatisticsAsync();
    }

    private async Task OnLibraryChangedAsync(Guid libraryId)
    {
        _selectedLibraryId = libraryId;
        await LoadStatisticsAsync();
    }

    private async Task LoadLibrariesAsync()
    {
        var result = await LibrariesService.FilterBy(null, null, null, 1, MaxLibraries, _loadCts.Token);
        if (result.IsSuccess && result.Value?.Items is not null)
        {
            _libraries = result.Value.Items.Select(LibraryUiDto.Convert).ToList();
        }
        else if (result.IsFailure)
        {
            Snackbar.Add("Failed to load libraries", Severity.Error);
            Logger.LogError("Statistics: failed to load libraries: {ErrorDescription}", result.Error!.Description);
        }
    }

    private async Task LoadStatisticsAsync()
    {
        var libraryId = _selectedLibraryId;

        await _loadCts.CancelAsync();
        _loadCts.Dispose();
        _loadCts = new CancellationTokenSource();
        var cancellationToken = _loadCts.Token;

        _isLoading = true;
        try
        {
            var result = await StatisticsService.Get(libraryId == Guid.Empty ? null : libraryId, cancellationToken);

            if (cancellationToken.IsCancellationRequested || libraryId != _selectedLibraryId)
            {
                return;
            }

            if (result.IsSuccess && result.Value is not null)
            {
                _statistics = StatisticsViewModel.From(result.Value, allLibraries: libraryId == Guid.Empty);
                _monthLabels = [.. _statistics.MonthLabels];
            }
            else if (result.IsFailure)
            {
                Snackbar.Add("Failed to load statistics", Severity.Error);
                Logger.LogError("Statistics: failed to load statistics for library {LibraryId}: {ErrorDescription}", libraryId, result.Error!.Description);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A newer load replaced this one: its result is stale.
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _isLoading = false;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _loadCts.CancelAsync();
        _loadCts.Dispose();
    }
}
