using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Series;

// The series of a library, each one a card with its number of volumes and what it misses (#1205).
public partial class LibrarySeriesPage : IAsyncDisposable
{
    [Inject] private ISeriesService SeriesService { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<LibrarySeriesPage> Logger { get; set; } = default!;

    [Parameter] public Guid LibraryId { get; set; }

    private readonly CancellationTokenSource _disposalCts = new();
    private Guid _shownLibraryId;
    private string? _libraryName;
    private List<SerieCardViewModel> _series = [];
    private SeriesFilter _filter = SeriesFilter.All;
    private bool _isLoading = true;

    private string? Subtitle => _isLoading ? null : $"{SerieDisplay.SeriesCount(_series.Count)} · Series";

    private int CountOf(SeriesFilter filter) => _series.Count(s => s.Matches(filter));

    protected override async Task OnParametersSetAsync()
    {
        if (_shownLibraryId == LibraryId)
        {
            return;
        }

        _shownLibraryId = LibraryId;
        _filter = SeriesFilter.All;
        await LoadAsync(LibraryId);
    }

    private async Task LoadAsync(Guid libraryId)
    {
        _isLoading = true;
        try
        {
            var library = await LibrariesService.GetById(libraryId.ToString());
            var series = await SeriesService.GetLibrarySeries(libraryId, _disposalCts.Token);
            if (libraryId != LibraryId)
            {
                return;
            }

            if (library.IsSuccess && series.IsSuccess)
            {
                _libraryName = library.Value!.Name;
                _series = series.Value!.Select(SerieCardViewModel.From).ToList();
            }
            else if (library.IsFailure || series.IsFailure)
            {
                var error = library.Error ?? series.Error;
                Snackbar.Add("Failed to load the series", Severity.Error);
                Logger.LogError("Failed to load the series of library {LibraryId}: {ErrorDescription}", libraryId, error?.Description);
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while loading.
        }
        finally
        {
            _isLoading = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
    }
}
