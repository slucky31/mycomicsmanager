using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Series;

// The volumes missing in the series of every library, to find them (#1205).
public partial class MissingVolumesPage : IAsyncDisposable
{
    [Inject] private ISeriesService SeriesService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<MissingVolumesPage> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _disposalCts = new();
    private MissingVolumesViewModel? _missing;
    // Guid.Empty: every library.
    private Guid _libraryId = Guid.Empty;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var result = await SeriesService.GetMissingVolumes(_disposalCts.Token);
            if (result.IsSuccess)
            {
                _missing = MissingVolumesViewModel.From(result.Value!);
            }
            else if (result.IsFailure)
            {
                Snackbar.Add("Failed to load the missing volumes", Severity.Error);
                Logger.LogError("Failed to load the missing volumes: {ErrorDescription}", result.Error!.Description);
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
