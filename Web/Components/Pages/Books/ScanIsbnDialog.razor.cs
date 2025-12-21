using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Web.Components.Pages.Books;

public partial class ScanIsbnDialog : IAsyncDisposable
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter] public EventCallback<string> OnIsbnScanned { get; set; }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<ScanIsbnDialog>? _dotNetObjectRef;
    private readonly string _videoElementId = $"video-scanner-{Guid.NewGuid():N}";
    private string _errorMessage = string.Empty;
    private bool _isbnDetected;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetObjectRef = DotNetObjectReference.Create(this);
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/isbnScanner.js");

            if (_jsModule != null)
            {
                try
                {
                    await _jsModule.InvokeVoidAsync("startScan", _videoElementId, _dotNetObjectRef);
                    _errorMessage = string.Empty;
                    StateHasChanged();
                }
                catch (JSException ex)
                {
                    _errorMessage = $"Failed to start camera: {ex.Message}";
                    StateHasChanged();
                }
            }
        }
    }

    private async Task StopScanning()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("stopScan");
        }
        StateHasChanged();
    }

    private async Task Cancel()
    {
        await StopScanning();
        MudDialog.Cancel();
    }

    [JSInvokable]
    public async Task OnIsbnScannedFromJs(string isbn)
    {
        _isbnDetected = true;
        StateHasChanged();
        await Task.Delay(500);
        await StopScanning();
        await OnIsbnScanned.InvokeAsync(isbn);
        MudDialog.Close(DialogResult.Ok(isbn));
    }

    [JSInvokable]
    public void OnScanErrorFromJs(string error)
    {
        _errorMessage = error;
        StateHasChanged();
    }

    [JSInvokable]
    public bool ValidateIsbn(string isbn)
    {
        return Application.Helpers.IsbnHelper.IsValidISBN(isbn);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("stopScan");
                await _jsModule.DisposeAsync();
                GC.SuppressFinalize(this);
            }
            catch (JSDisconnectedException)
            {
                // Circuit is gone, ignore or log if needed
            }
        }
        _dotNetObjectRef?.Dispose();
    }
}
