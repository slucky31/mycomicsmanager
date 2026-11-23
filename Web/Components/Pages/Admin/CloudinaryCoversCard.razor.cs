using System.Globalization;
using Application.Books.Covers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Web.Services;

namespace Web.Components.Pages.Admin;

// Moves the covers to the image of their book, then deletes the images no book uses (#1225).
public partial class CloudinaryCoversCard
{
    private const int SampleSize = 3;

    [Inject] private ICloudinaryCoversService CoversService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private ILogger<CloudinaryCoversCard> Logger { get; set; } = default!;

    private int? _toMove;
    private bool _isCounting;
    private bool _isStarting;
    private bool _isFinding;
    private bool _isDeleting;
    private UnusedCovers? _unused;
    private DeletedCovers? _deleted;

    protected override Task OnInitializedAsync() => CountToMoveAsync();

    internal async Task CountToMoveAsync()
    {
        _isCounting = true;
        try
        {
            var result = await CoversService.CountToMoveAsync();
            if (result.IsSuccess)
            {
                _toMove = result.Value;
            }
            else if (result.IsFailure)
            {
                _toMove = null;
                Snackbar.Add(result.Error?.Description ?? "Unable to count the old covers.", Severity.Error);
                Logger.LogError("Settings: old covers not counted: {ErrorDescription}", result.Error?.Description);
            }
        }
        finally
        {
            _isCounting = false;
        }
    }

    internal async Task StartMoveAsync()
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;
        try
        {
            var result = await CoversService.StartMoveAsync();
            if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to move the covers.", Severity.Error);
                Logger.LogError("Settings: covers not moved: {ErrorDescription}", result.Error?.Description);
            }
            else if (result.Value == 0)
            {
                Snackbar.Add("No cover to move.", Severity.Info);
            }
            else
            {
                Snackbar.Add($"{result.Value} cover(s) will be moved in the background.", Severity.Success);
            }
        }
        finally
        {
            _isStarting = false;
        }
    }

    internal async Task FindUnusedAsync()
    {
        if (_isFinding)
        {
            return;
        }

        _isFinding = true;
        _deleted = null;
        try
        {
            var result = await CoversService.FindUnusedAsync();
            if (result.IsSuccess)
            {
                _unused = result.Value;
            }
            else if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to find the unused images.", Severity.Error);
                Logger.LogError("Settings: unused images not found: {ErrorDescription}", result.Error?.Description);
            }
        }
        finally
        {
            _isFinding = false;
        }
    }

    internal async Task DeleteAsync()
    {
        if (_isDeleting || _unused is null)
        {
            return;
        }

        _isDeleting = true;
        try
        {
            var result = await CoversService.DeleteUnusedAsync(_unused.PublicIds);
            if (result.IsSuccess)
            {
                _deleted = result.Value;
                _unused = null;
            }
            else if (result.IsFailure)
            {
                Snackbar.Add(result.Error?.Description ?? "Unable to delete the unused images.", Severity.Error);
                Logger.LogError("Settings: unused images not deleted: {ErrorDescription}", result.Error?.Description);
            }
        }
        finally
        {
            _isDeleting = false;
        }
    }

    internal void Cancel() => _unused = null;

    private static string ImageCount(int count) => count == 1 ? "1 image" : $"{count.ToString("N0", CultureInfo.InvariantCulture)} images";

    private static string SizeLabel(long bytes) => bytes >= 1_000_000
        ? $"{(bytes / 1_000_000d).ToString("0.#", CultureInfo.InvariantCulture)} MB"
        : $"{(bytes / 1_000d).ToString("0", CultureInfo.InvariantCulture)} KB";
}
