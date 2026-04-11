using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Serilog;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages;

public partial class Import : IAsyncDisposable
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IImportService ImportService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? LibraryId { get; set; }

    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;

    private List<LibraryUiDto> _digitalLibraries = [];
    private Guid _selectedLibraryId = Guid.Empty;
    private Guid _lastLoadedLibraryId = Guid.Empty;

    private List<ImportJobViewModel> _jobs = [];
    private readonly List<string> _uploadErrors = [];

    private bool _isLoadingJobs;
    private bool _isUploading;
    private string? _loadError;

    private System.Threading.Timer? _pollTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadLibrariesAsync();

        if (Guid.TryParse(LibraryId, out var preselectedId)
            && _digitalLibraries.Any(l => l.Id == preselectedId))
        {
            _selectedLibraryId = preselectedId;
        }
        else if (_digitalLibraries.Count > 0)
        {
            _selectedLibraryId = _digitalLibraries[0].Id;
        }

        if (_selectedLibraryId != Guid.Empty)
        {
            await LoadJobsAsync(_selectedLibraryId);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_selectedLibraryId != _lastLoadedLibraryId && _selectedLibraryId != Guid.Empty)
        {
            await LoadJobsAsync(_selectedLibraryId);
        }
    }

    private async Task LoadLibrariesAsync()
    {
        var result = await LibrariesService.FilterBy(null, null, null, 1, 200);
        if (result.IsSuccess && result.Value?.Items is not null)
        {
            _digitalLibraries = result.Value.Items
                .Where(l => l.BookType == LibraryBookType.Digital)
                .Select(LibraryUiDto.Convert)
                .ToList();
        }
        else if (result.IsFailure)
        {
            _loadError = "Impossible de charger les librairies.";
            Log.Error("Import: failed to load libraries: {Error}", result.Error?.Description);
        }
    }

    private async Task LoadJobsAsync(Guid libraryId)
    {
        _isLoadingJobs = true;
        _uploadErrors.Clear();
        StateHasChanged();

        var capturedLibraryId = libraryId;
        var result = await ImportService.GetImportJobsAsync(capturedLibraryId);

        if (_selectedLibraryId != capturedLibraryId)
        {
            _isLoadingJobs = false;
            return;
        }

        if (result.IsSuccess)
        {
            _jobs = result.Value!.OrderByDescending(j => j.CreatedAt).ToList();
            _lastLoadedLibraryId = capturedLibraryId;
        }
        else if (result.IsFailure)
        {
            _jobs = [];
            Log.Error("Import: failed to load jobs for library {LibraryId}: {Error}", capturedLibraryId, result.Error?.Description);
        }

        _isLoadingJobs = false;
        StartPollingIfNeeded();
        StateHasChanged();
    }

    private Task OpenFilePickerAsync() => _fileUpload?.OpenFilePickerAsync() ?? Task.CompletedTask;

    private async Task OnLibraryChangedAsync(Guid newLibraryId)
    {
        _selectedLibraryId = newLibraryId;
        StopPolling();
        await LoadJobsAsync(newLibraryId);
    }

    private async Task OnFilesSelectedAsync(IReadOnlyList<IBrowserFile> files)
    {
        if (files is null || files.Count == 0)
        {
            return;
        }

        _isUploading = true;
        _uploadErrors.Clear();
        StateHasChanged();

        var capturedLibraryId = _selectedLibraryId;

        foreach (var file in files)
        {
            var result = await ImportService.UploadAndCreateJobAsync(file, capturedLibraryId);

            if (_selectedLibraryId != capturedLibraryId)
            {
                break;
            }

            if (result.IsSuccess)
            {
                _jobs.Insert(0, result.Value!);
            }
            else if (result.IsFailure)
            {
                _uploadErrors.Add($"{file.Name}: {result.Error?.Description ?? "Erreur inconnue"}");
                Log.Error("Import: upload failed for {FileName}: {Error}", file.Name, result.Error?.Description);
            }
        }

        _isUploading = false;
        StartPollingIfNeeded();
        StateHasChanged();

        if (_uploadErrors.Count == 0 && files.Count > 0)
        {
            Snackbar.Add($"{files.Count} fichier(s) envoyé(s) avec succès", Severity.Success);
        }
    }

    private void StartPollingIfNeeded()
    {
        var hasActiveJobs = _jobs.Any(j => !j.IsTerminal);
        if (!hasActiveJobs)
        {
            StopPolling();
            return;
        }

        if (_pollTimer is not null)
        {
            return;
        }

        _pollTimer = new System.Threading.Timer(
            _ => InvokeAsync(PollJobsAsync),
            null,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(3));
    }

    private async Task PollJobsAsync()
    {
        var capturedLibraryId = _selectedLibraryId;
        var result = await ImportService.GetImportJobsAsync(capturedLibraryId);

        if (_selectedLibraryId != capturedLibraryId)
        {
            return;
        }

        if (result.IsSuccess)
        {
            _jobs = result.Value!.OrderByDescending(j => j.CreatedAt).ToList();

            if (!_jobs.Any(j => !j.IsTerminal))
            {
                StopPolling();
            }
        }
        else if (result.IsFailure)
        {
            Log.Error("Import: polling failed for library {LibraryId}: {Error}", capturedLibraryId, result.Error?.Description);
        }

        StateHasChanged();
    }

    private void StopPolling()
    {
        var timer = _pollTimer;
        _pollTimer = null;
        timer?.Dispose();
    }

    private async Task DeleteJobAsync(Guid jobId)
    {
        var result = await ImportService.DeleteImportJobAsync(jobId);
        if (result.IsSuccess)
        {
            _jobs.RemoveAll(j => j.Id == jobId);
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result.Error?.Description ?? "Impossible de supprimer le job", Severity.Error);
        }
    }

    private async Task ForceFailJobAsync(Guid jobId)
    {
        var result = await ImportService.ForceFailImportJobAsync(jobId);
        if (result.IsSuccess)
        {
            var capturedLibraryId = _selectedLibraryId;
            var refreshResult = await ImportService.GetImportJobsAsync(capturedLibraryId);
            if (refreshResult.IsSuccess && _selectedLibraryId == capturedLibraryId)
            {
                _jobs = refreshResult.Value!.OrderByDescending(j => j.CreatedAt).ToList();
            }
            Snackbar.Add("Import marqué comme échoué.", Severity.Warning);
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result.Error?.Description ?? "Impossible de forcer l'échec du job", Severity.Error);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        StopPolling();
        await Task.CompletedTask;
    }
}
