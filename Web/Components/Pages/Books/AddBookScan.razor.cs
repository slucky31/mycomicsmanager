using Application.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Infrastructure;
using Web.Models;
using Web.Services;

namespace Web.Components.Pages.Books;

// Full screen barcode scanner (A2); one clear message when the camera cannot be used (A3).
public sealed partial class AddBookScan : IAsyncDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private ILogger<AddBookScan> Logger { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? LibraryId { get; set; }

    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<AddBookScan>? _dotNetObjectRef;
    private readonly string _videoElementId = $"video-scanner-{Guid.NewGuid():N}";

    private Guid? _libraryId;
    private string? _libraryName;
    private bool _isScanning;
    private bool _isbnDetected;
    private bool _cameraUnavailable;
    private bool _hasTorch;
    private bool _torchOn;

    protected override async Task OnInitializedAsync()
    {
        _libraryId = AddBookLinks.ParseLibraryId(LibraryId);
        _libraryName = await LibrariesService.NameOfAsync(_libraryId);
    }

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
            _dotNetObjectRef ??= DotNetObjectReference.Create(this);
            _jsModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", AppVersion.Versioned("./js/isbnScanner.js"));

            await _jsModule.InvokeVoidAsync("startScan", _videoElementId, _dotNetObjectRef);
            _isScanning = true;
            _cameraUnavailable = false;
            _hasTorch = await _jsModule.InvokeAsync<bool>("hasTorch");
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to start the barcode scanner");
            _isScanning = false;
            _cameraUnavailable = true;
        }

        StateHasChanged();
    }

    private async Task RetryAsync()
    {
        _cameraUnavailable = false;
        await StartScanningAsync();
    }

    private async Task ToggleTorchAsync()
    {
        if (_jsModule is null)
        {
            return;
        }

        try
        {
            _torchOn = await _jsModule.InvokeAsync<bool>("setTorch", !_torchOn);
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Unable to switch the flashlight");
            _hasTorch = false;
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
            catch (JSException)
            {
                // Ignore
            }
        }
        _isScanning = false;
        _torchOn = false;
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

        NavigationManager.NavigateTo(AddBookLinks.LookUp(isbn, _libraryId));
    }

    // The technical reason goes to the log; the page shows a single message (A3).
    [JSInvokable]
    public void OnScanErrorFromJs(string error)
    {
        Logger.LogWarning("Barcode scanner error: {Error}", error);
        _isScanning = false;
        _cameraUnavailable = true;
        StateHasChanged();
    }

    [JSInvokable]
    public bool ValidateIsbn(string isbn)
    {
        return IsbnHelper.IsValidISBN(isbn);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            if (_isScanning)
            {
                await StopScanningAsync();
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
