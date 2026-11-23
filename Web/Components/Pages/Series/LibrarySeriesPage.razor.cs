using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Series;

// The series of a library, each one a card with its number of volumes and what it misses (#1205), only the rows on
// screen rendered, opened again with the filter and on the series left (#1366).
public partial class LibrarySeriesPage : IAsyncDisposable
{
    [Inject] private ISeriesService SeriesService { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private PopupAnchorLocator PopupAnchorLocator { get; set; } = default!;
    [Inject] private ILogger<LibrarySeriesPage> Logger { get; set; } = default!;
    [Inject] private SeriesStateService SeriesStateService { get; set; } = default!;

    [Parameter] public Guid LibraryId { get; set; }

    public const string Msg_LoadFailed = "Failed to load the series";

    private const string ViewButtonId = "series-view";
    private static readonly Func<SerieCardViewModel, object> KeyOf = serie => serie.Href;

    private readonly CancellationTokenSource _disposalCts = new();
    private Guid _shownLibraryId;
    private Library? _library;
    private string? _libraryName;
    private List<SerieCardViewModel> _series = [];
    private SeriesFilter _filter = SeriesFilter.All;
    private ListRows<SerieCardViewModel> _rows = new([], nameof(SeriesFilter.All));
    private int _firstSerie;
    private bool _isLoading = true;
    private bool _isViewSheetOpen;
    private PopupAnchor? _sheetAnchor;

    private string? Subtitle => _isLoading ? null : $"{SerieDisplay.SeriesCount(_series.Count)} · Series";

    private int CountOf(SeriesFilter filter) => _series.Count(s => s.Matches(filter));

    protected override async Task OnParametersSetAsync()
    {
        if (_shownLibraryId == LibraryId)
        {
            return;
        }

        SaveState();
        _shownLibraryId = LibraryId;
        var saved = SeriesStateService.LoadSeries(LibraryId);
        _filter = saved?.Filter ?? SeriesFilter.All;
        _firstSerie = saved?.FirstSerie ?? 0;
        await LoadAsync(LibraryId);
    }

    // The series left are shown again with their filter, from the first one that was on screen.
    private void SaveState()
    {
        if (_shownLibraryId != Guid.Empty)
        {
            SeriesStateService.SaveSeries(_shownLibraryId, new SeriesPageState(_filter, _firstSerie));
        }
    }

    // Another filter shows its series from the top.
    private void ChooseFilter(SeriesFilter filter)
    {
        _filter = filter;
        _firstSerie = 0;
        ShowSeries();
    }

    private void ShowSeries() => _rows = new ListRows<SerieCardViewModel>([.. _series.Where(s => s.Matches(_filter))], _filter.ToString());

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
                _library = library.Value!;
                _libraryName = _library.Name;
                _series = series.Value!.Select(SerieCardViewModel.From).ToList();
                ShowSeries();
            }
            else if (library.IsFailure || series.IsFailure)
            {
                var error = library.Error ?? series.Error;
                Snackbar.Add(Msg_LoadFailed, Severity.Error);
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

    private async Task OpenViewSheetAsync()
    {
        _sheetAnchor = await PopupAnchorLocator.AnchorOfAsync(ViewButtonId, _disposalCts.Token);
        _isViewSheetOpen = true;
    }

    private void CloseViewSheet() => _isViewSheetOpen = false;

    // The series are one of the views of the library (#1210): Covers or List becomes the view it opens with (#1212).
    private async Task ChooseViewAsync(LibraryView view)
    {
        CloseViewSheet();
        if (_library is not { } library)
        {
            return;
        }

        try
        {
            var result = await LibrariesService.Update(new UpdateLibraryRequest(
                LibraryId.ToString(),
                null,
                library.Color,
                library.Icon,
                DefaultView: view), _disposalCts.Token);

            if (result.IsSuccess)
            {
                NavigationManager.NavigateTo($"/libraries/{LibraryId}");
            }
            else
            {
                Snackbar.Add("Failed to save the view", Severity.Error);
                Logger.LogError("Failed to save the view of library {LibraryId}: {ErrorDescription}", LibraryId, result.Error!.Description);
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while saving.
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        SaveState();
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
    }
}
