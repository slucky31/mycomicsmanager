using Application.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Web.Components.Pages.Books;

public sealed partial class AddBookScan : IAsyncDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<AddBookScan>? _dotNetObjectRef;
    private readonly string _videoElementId = $"video-scanner-{Guid.NewGuid():N}";

    private bool _isScanning;
    private bool _isbnDetected;
    private string _errorMessage = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await StartScanningAsync();
        }
    }

    private async Task StartScanningAsync()
    {
        try
        {
            _dotNetObjectRef = DotNetObjectReference.Create(this);
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/isbnScanner.js");

            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("startScan", _videoElementId, _dotNetObjectRef);
                _isScanning = true;
                _errorMessage = string.Empty;
                StateHasChanged();
            }
        }
        catch (JSException ex)
        {
            _errorMessage = $"Camera error: {ex.Message}";
            _isScanning = false;
            StateHasChanged();
        }
    }

    private async Task StopScanningAsync()
    {
        if (_jsModule != null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("stopScan");
            }
            catch (JSDisconnectedException)
            {
                // Ignore
            }
        }
        _isScanning = false;
    }

    [JSInvokable]
    public async Task OnIsbnScannedFromJsAsync(string isbn)
    {
        if (_isbnDetected)
        {
            return;
        }

        _isbnDetected = true;
        StateHasChanged();

        // Small delay to show visual feedback
        await Task.Delay(300);

        await StopScanningAsync();

        if (!IsbnHelper.IsValidISBN(isbn))
        {
            Snackbar.Add($"Invalid ISBN scanned: {isbn}", Severity.Error);
            _isbnDetected = false;
            StateHasChanged();
            return;
        }
        
        NavigationManager.NavigateTo($"/books/add/form?isbn={Uri.EscapeDataString(isbn)}");
    }

    [JSInvokable]
    public void OnScanErrorFromJs(string error)
    {
        _errorMessage = error;
        _isScanning = false;
        StateHasChanged();
    }

    [JSInvokable]
    public bool ValidateIsbn(string isbn)
    {
        return IsbnHelper.IsValidISBN(isbn);
    }

    private async Task GoBackAsync()
    {
        await StopScanningAsync();
        NavigationManager.NavigateTo("/books/add");
    }

    private async Task GoToManualInputAsync()
    {
        await StopScanningAsync();
        NavigationManager.NavigateTo("/books/add/manual");
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            if (_isScanning)
            {
                try
                {
                    await _jsModule.InvokeVoidAsync("stopScan");
                }
                catch (JSDisconnectedException)
                {
                    // Ignore
                }
                catch (JSException)
                {
                    // Ignore
                }
            }

            try
            {
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Ignore
            }
            catch (JSException)
            {
                // Ignore
            }
        }

        _dotNetObjectRef?.Dispose();
    }
}
