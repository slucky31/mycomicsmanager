using Domain.Libraries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Web.Extensions;
using Web.Models;
using Web.Services;
using Web.Validators;

namespace Web.Components.Pages;

public partial class Import : IAsyncDisposable
{
    [Inject] private ILibrariesService LibrariesService { get; set; } = default!;
    [Inject] private IImportService ImportService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ILogger<Import> Logger { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? LibraryId { get; set; }

    private List<LibraryUiDto> _digitalLibraries = [];
    private Guid _selectedLibraryId = Guid.Empty;
    private Guid _lastLoadedLibraryId = Guid.Empty;

    private List<ImportJobViewModel> _jobs = [];
    private readonly List<string> _uploadErrors = [];

    private bool _isLoadingJobs;
    private bool _isUploading;
    private string? _loadError;

    private System.Threading.Timer? _pollTimer;
    private readonly CancellationTokenSource _pollingCts = new();

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
            // The files to sort usually go to the Imported library, where the feed imports add theirs.
            _selectedLibraryId = (_digitalLibraries.Find(l => Library.IsImportedName(l.Name)) ?? _digitalLibraries[0]).Id;
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
        var result = await LibrariesService.FilterBy(null, null, null, 1, 200, _pollingCts.Token);
        if (result.IsSuccess && result.Value?.Items is not null)
        {
            _digitalLibraries = result.Value.Items
                .Where(l => l.BookType == LibraryBookType.Digital)
                .Select(LibraryUiDto.Convert)
                .ToList();
        }
        else if (result.IsFailure)
        {
            _loadError = "Unable to load the libraries.";
            Logger.LogError("Import: failed to load libraries: {Error}", result.Error!.Description);
        }
    }

    private async Task LoadJobsAsync(Guid libraryId)
    {
        _isLoadingJobs = true;
        _uploadErrors.Clear();
        StateHasChanged();

        var capturedLibraryId = libraryId;
        try
        {
            var result = await ImportService.GetImportJobsAsync(capturedLibraryId, _pollingCts.Token);

            if (_selectedLibraryId != capturedLibraryId)
            {
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
                Snackbar.Add(result.Error?.Description ?? "Unable to load the imports.", Severity.Error);
                Logger.LogError("Import: failed to load jobs for library {LibraryId}: {Error}", capturedLibraryId, result.Error!.Description);
            }

            StartPollingIfNeeded();
        }
        finally
        {
            _isLoadingJobs = false;
            StateHasChanged();
        }
    }

    private async Task OnLibraryChangedAsync(Guid newLibraryId)
    {
        _selectedLibraryId = newLibraryId;
        StopPolling();
        await LoadJobsAsync(newLibraryId);
    }

    internal async Task OnFilesSelectedAsync(IReadOnlyList<IBrowserFile> files)
    {
        if (files is null || files.Count == 0)
        {
            return;
        }

        _isUploading = true;
        _uploadErrors.Clear();
        StateHasChanged();

        var capturedLibraryId = _selectedLibraryId;
        try
        {
            foreach (var file in files)
            {
                var result = await ImportService.UploadAndCreateJobAsync(file, capturedLibraryId, _pollingCts.Token);

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
                    _uploadErrors.Add($"{file.Name}: {result.Error?.Description ?? "Unknown error"}");
                    Logger.LogError("Import: upload failed for {FileName}: {Error}", file.Name, result.Error!.Description);
                }
            }

            StartPollingIfNeeded();

            if (_uploadErrors.Count == 0)
            {
                Snackbar.Add($"{files.Count} file(s) uploaded successfully", Severity.Success);
            }
        }
        finally
        {
            _isUploading = false;
            StateHasChanged();
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

    private int _pollRequestId;

    internal async Task PollJobsAsync()
    {
        if (_pollingCts.IsCancellationRequested)
        {
            return;
        }
        var capturedLibraryId = _selectedLibraryId;
        var requestId = ++_pollRequestId;
        var result = await ImportService.GetImportJobsAsync(capturedLibraryId, _pollingCts.Token);

        // Discard this response if the library changed or a newer poll was issued while awaiting.
        if (_selectedLibraryId != capturedLibraryId || requestId != _pollRequestId)
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
            Logger.LogError("Import: polling failed for library {LibraryId}: {Error}", capturedLibraryId, result.Error!.Description);
        }

        StateHasChanged();
    }

    private void StopPolling()
    {
        var timer = _pollTimer;
        _pollTimer = null;
        timer?.Dispose();
    }

    private bool _isDeletingCompleted;

    // Only the successful imports: a failed one is retried or abandoned from its card.
    internal async Task DeleteCompletedJobsAsync()
    {
        var completedJobIds = _jobs.Where(j => j.IsSucceeded).Select(j => j.Id).ToList();
        if (completedJobIds.Count == 0 || _isDeletingCompleted)
        {
            return;
        }

        // Same confirmation as the sweep of the feed imports: what goes, and what is kept.
        var kept = _jobs.Count - completedJobIds.Count;
        var message = $"Delete {completedJobIds.Count} successful import(s) from the list? The imported books are kept.";
        if (kept > 0)
        {
            message += $" {kept} import(s) running or failed will be kept.";
        }

        if (!await DialogService.ShowConfirmationAsync("Clear successful imports", message, "Clear successful imports"))
        {
            return;
        }

        _isDeletingCompleted = true;
        StateHasChanged();
        try
        {
            var deleted = await DeleteJobsAsync(completedJobIds);
            ShowDeletionSummary(deleted, completedJobIds.Count - deleted, kept);
        }
        finally
        {
            _isDeletingCompleted = false;
            StateHasChanged();
        }
    }

    private async Task<int> DeleteJobsAsync(List<Guid> jobIds)
    {
        var deleted = 0;
        foreach (var jobId in jobIds)
        {
            var result = await ImportService.DeleteImportJobAsync(jobId, _pollingCts.Token);
            if (result.IsSuccess)
            {
                _jobs.RemoveAll(j => j.Id == jobId);
                deleted++;
            }
            else
            {
                Logger.LogError("Import: failed to delete completed job {JobId}: {Error}", jobId, result.Error!.Description);
            }
        }

        return deleted;
    }

    private void ShowDeletionSummary(int deleted, int failed, int kept)
    {
        if (failed > 0)
        {
            Snackbar.Add($"{deleted} import(s) deleted, unable to delete {failed}.", Severity.Error);
        }
        else if (kept > 0)
        {
            Snackbar.Add($"{deleted} import(s) deleted, {kept} kept (running or failed).", Severity.Info);
        }
        else
        {
            Snackbar.Add($"{deleted} import(s) deleted.", Severity.Success);
        }
    }

    internal async Task DeleteJobAsync(Guid jobId)
    {
        var job = _jobs.Find(j => j.Id == jobId);
        if (job is null)
        {
            return;
        }

        var confirmed = await DialogService.ShowConfirmationAsync(
            "Delete import",
            $"Delete the import of \"{job.OriginalFileName}\" from the list? The imported book is kept.",
            "Delete import");
        if (!confirmed)
        {
            return;
        }

        var result = await ImportService.DeleteImportJobAsync(jobId, _pollingCts.Token);
        if (result.IsSuccess)
        {
            _jobs.RemoveAll(j => j.Id == jobId);
            Snackbar.Add("Import deleted.", Severity.Success);
        }
        else
        {
            Logger.LogError("Import: failed to delete job {JobId}: {Error}", jobId, result.Error?.Description);
            Snackbar.Add(result.Error?.Description ?? "Unable to delete the import.", Severity.Error);
        }

        StateHasChanged();
    }

    internal async Task RetryJobAsync(Guid jobId)
    {
        var result = await ImportService.RetryImportJobAsync(jobId, _pollingCts.Token);
        if (result.IsSuccess)
        {
            await RefreshJobsAsync();
            StartPollingIfNeeded();
            Snackbar.Add("Import restarted.", Severity.Success);
        }
        else
        {
            Logger.LogError("Import: failed to retry job {JobId}: {Error}", jobId, result.Error?.Description);
            Snackbar.Add(result.Error?.Description ?? "Unable to retry the import", Severity.Error);
        }

        StateHasChanged();
    }

    internal async Task AbandonJobAsync(ImportJobViewModel job)
    {
        var confirmed = await DialogService.ShowConfirmationAsync(
            "Abandon import",
            $"Abandon the import of \"{job.OriginalFileName}\"? It is removed from the list and its file is deleted from the errors directory; no book is created.",
            "Abandon import");
        if (!confirmed)
        {
            return;
        }

        var result = await ImportService.AbandonImportJobAsync(job.Id, _pollingCts.Token);
        if (result.IsSuccess)
        {
            _jobs.RemoveAll(j => j.Id == job.Id);
            Snackbar.Add("Import abandoned.", Severity.Success);
        }
        else
        {
            Logger.LogError("Import: failed to abandon job {JobId}: {Error}", job.Id, result.Error?.Description);
            Snackbar.Add(result.Error?.Description ?? "Unable to abandon the import", Severity.Error);
        }

        StateHasChanged();
    }

    private async Task RefreshJobsAsync()
    {
        var capturedLibraryId = _selectedLibraryId;
        var refreshResult = await ImportService.GetImportJobsAsync(capturedLibraryId, _pollingCts.Token);
        if (_selectedLibraryId != capturedLibraryId)
        {
            return;
        }

        if (refreshResult.IsSuccess)
        {
            _jobs = refreshResult.Value!.OrderByDescending(j => j.CreatedAt).ToList();
        }
        else if (refreshResult.IsFailure)
        {
            Logger.LogError("Import: refresh failed for library {LibraryId}: {Error}", capturedLibraryId, refreshResult.Error!.Description);
        }
    }

    private async Task ForceFailJobAsync(Guid jobId)
    {
        var result = await ImportService.ForceFailImportJobAsync(jobId, _pollingCts.Token);
        if (result.IsSuccess)
        {
            await RefreshJobsAsync();
            Snackbar.Add("Import marked as failed.", Severity.Warning);
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result.Error?.Description ?? "Unable to force the job to fail", Severity.Error);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816", Justification = "No finalizer; S3971 prohibits GC.SuppressFinalize in DisposeAsync.")]
    public async ValueTask DisposeAsync()
    {
        await _pollingCts.CancelAsync();
        _pollingCts.Dispose();
        StopPolling();
    }
}
