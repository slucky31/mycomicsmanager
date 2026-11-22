using Application.FeedImports.HostStatus;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Admin;

public partial class HostStatusPage : IAsyncDisposable
{
    [Inject] private IFeedImportService FeedImportService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<HostStatusPage> Logger { get; set; } = default!;

    private readonly CancellationTokenSource _disposeCts = new();
    private HostStatusOverview _overview = HostStatusOverview.Empty;
    private HostStatusPageViewModel _view = HostStatusPageViewModel.Empty;
    private bool _isLoading;
    private int _downloadedAfterRetry;
    private bool _downOnly;
    // The host whose history is shown; every host when null.
    private string? _host;

    protected override Task OnInitializedAsync() => LoadAsync();

    internal async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            var result = await FeedImportService.GetHostStatusOverviewAsync(_disposeCts.Token);
            if (result.IsSuccess)
            {
                _overview = result.Value!;
                Rebuild();
                _downloadedAfterRetry = await FeedImportService.CountDownloadedAfterAutoRetryAsync(_disposeCts.Token);
            }
            else if (result.IsFailure)
            {
                Snackbar.Add("Unable to load the Debrid-Link hosts.", Severity.Error);
                Logger.LogError("Hosts: failed to load the host states: {ErrorDescription}", result.Error!.Description);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    internal void SetDownOnly(bool downOnly)
    {
        _downOnly = downOnly;
        Rebuild();
    }

    internal void SelectHost(string? host)
    {
        _host = host == _host ? null : host;
        Rebuild();
    }

    private void Rebuild() => _view = HostStatusPageViewModel.From(_overview, _downOnly, _host);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _disposeCts.CancelAsync();
        _disposeCts.Dispose();
    }
}
