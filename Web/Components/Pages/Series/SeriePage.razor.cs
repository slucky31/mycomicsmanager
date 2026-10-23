using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Series;

// A series of a library: how complete it is, and its volumes with the missing ones in their place (#1205).
public partial class SeriePage : IAsyncDisposable
{
    [Inject] private ISeriesService SeriesService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<SeriePage> Logger { get; set; } = default!;

    [Parameter] public Guid LibraryId { get; set; }

    [SupplyParameterFromQuery(Name = "name")]
    public string? Name { get; set; }

    private readonly CancellationTokenSource _disposalCts = new();
    private (Guid LibraryId, string? Name) _shown;
    private SeriePageViewModel? _serie;
    private bool _isLoading = true;
    private bool _isEditing;
    private bool _isSaving;
    private readonly SerieTotalForm _form = new();

    protected override async Task OnParametersSetAsync()
    {
        if (_shown == (LibraryId, Name))
        {
            return;
        }

        _shown = (LibraryId, Name);
        _isEditing = false;
        _isLoading = true;
        try
        {
            await LoadAsync();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadAsync()
    {
        var shown = _shown;
        if (string.IsNullOrWhiteSpace(shown.Name))
        {
            NavigationManager.NavigateTo(SerieDisplay.SeriesHref(shown.LibraryId));
            return;
        }

        try
        {
            var result = await SeriesService.GetSerie(shown.LibraryId, shown.Name, _disposalCts.Token);
            if (shown != _shown)
            {
                return;
            }

            if (result.IsSuccess)
            {
                _serie = SeriePageViewModel.From(result.Value!);
            }
            else if (result.IsFailure)
            {
                Snackbar.Add("Series not found", Severity.Error);
                Logger.LogError("Failed to load the series {Serie} of library {LibraryId}: {ErrorDescription}", shown.Name, shown.LibraryId, result.Error!.Description);
                NavigationManager.NavigateTo(SerieDisplay.SeriesHref(shown.LibraryId));
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while loading.
        }
    }

    private void StartEdit()
    {
        _form.TotalVolumes = _serie?.TotalVolumes;
        _form.IsFinished = _serie?.IsFinished ?? false;
        _isEditing = true;
    }

    private void CancelEdit() => _isEditing = false;

    private async Task SaveAsync()
    {
        if (_serie is null)
        {
            return;
        }

        _isSaving = true;
        try
        {
            var result = await SeriesService.SetTotal(LibraryId, _serie.Name, _form.TotalVolumes, _form.IsFinished, _disposalCts.Token);
            if (result.IsSuccess)
            {
                _isEditing = false;
                await LoadAsync();
            }
            else if (result.IsFailure)
            {
                Snackbar.Add(result.Error!.Description ?? "Failed to save the volumes", Severity.Error);
                Logger.LogError("Failed to save the volumes of the series {Serie} of library {LibraryId}: {ErrorDescription}", _serie.Name, LibraryId, result.Error.Description);
            }
        }
        catch (OperationCanceledException)
        {
            // Page left while saving.
        }
        finally
        {
            _isSaving = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _disposalCts.CancelAsync();
        _disposalCts.Dispose();
    }

    private sealed class SerieTotalForm
    {
        public int? TotalVolumes { get; set; }

        public bool IsFinished { get; set; }
    }
}
