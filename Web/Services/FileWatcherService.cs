using Application.Abstractions.Messaging;
using Application.ImportJobs;
using Application.ImportJobs.Create;
using Application.Interfaces;
using Domain.ImportJobs;
using Domain.Libraries;
using Domain.Primitives;
using Microsoft.Extensions.Options;

namespace Web.Services;

public sealed class FileWatcherService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IImportJobEnqueuer _enqueuer;
    private readonly ImportSettings _settings;
    private readonly HashSet<string> _supportedExtensions;
    private FileSystemWatcher? _watcher;
    private Timer? _pollingTimer;

    private static Serilog.ILogger Log => Serilog.Log.ForContext<FileWatcherService>();

    public FileWatcherService(
        IServiceScopeFactory scopeFactory,
        IImportJobEnqueuer enqueuer,
        IOptions<ImportSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _enqueuer = enqueuer;
        _settings = settings.Value;
        _supportedExtensions = new HashSet<string>(_settings.SupportedExtensions, StringComparer.OrdinalIgnoreCase);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_settings.ImportDirectory);

        // Scan existing files on startup
        await ScanDirectoryAsync(_settings.ImportDirectory, cancellationToken);

        // File system watcher for real-time detection
        _watcher = new FileSystemWatcher(_settings.ImportDirectory)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _watcher.Created += OnFileSystemEvent;

        // Polling timer as fallback (FileSystemWatcher is unreliable on some OS/filesystems)
        var intervalMs = _settings.PollingIntervalSeconds * 1000;
        _pollingTimer = new Timer(
            async _ => await ScanDirectoryAsync(_settings.ImportDirectory, CancellationToken.None),
            null,
            intervalMs,
            intervalMs);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pollingTimer?.Change(Timeout.Infinite, 0);
        _watcher?.Dispose();
        _watcher = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _pollingTimer?.Dispose();
        _watcher?.Dispose();
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Created) { return; }
        _ = ProcessFileAsync(e.FullPath, CancellationToken.None);
    }

    private async Task ScanDirectoryAsync(string rootDir, CancellationToken ct)
    {
        if (!Directory.Exists(rootDir)) { return; }

        foreach (var subDir in Directory.GetDirectories(rootDir))
        {
            foreach (var file in Directory.GetFiles(subDir))
            {
                await ProcessFileAsync(file, ct);
            }
        }
    }

    internal async Task ProcessFileAsync(string filePath, CancellationToken ct)
    {
        var extension = Path.GetExtension(filePath);
        if (!_supportedExtensions.Contains(extension))
        {
            Log.Debug("Ignoring unsupported file: {FilePath}", filePath);
            return;
        }

        // Library ID is the name of the immediate parent directory
        var parentDir = Path.GetDirectoryName(filePath) ?? string.Empty;
        var parentName = Path.GetFileName(parentDir);
        if (!Guid.TryParse(parentName, out var libraryId))
        {
            Log.Debug("Ignoring file at root or non-GUID subdirectory: {FilePath}", filePath);
            return;
        }

        if (!await WaitForFileReadyAsync(filePath, ct))
        {
            Log.Warning("File never became ready (still locked): {FilePath}", filePath);
            return;
        }

        if (!File.Exists(filePath))
        {
            Log.Debug("File disappeared before processing: {FilePath}", filePath);
            return;
        }

        // Load the library to obtain its owning UserId
        Guid userId;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var libraryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Library, Guid>>();
            var library = await libraryRepository.GetByIdAsync(libraryId);
            if (library is null)
            {
                Log.Warning("Library {LibraryId} not found for file {FilePath}", libraryId, filePath);
                return;
            }

            userId = library.UserId;
        }

        var fileInfo = new FileInfo(filePath);
        var command = new CreateImportJobCommand(
            Path.GetFileName(filePath),
            filePath,
            fileInfo.Length,
            libraryId,
            userId);

        Result<ImportJob> result;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider
                .GetRequiredService<ICommandHandler<CreateImportJobCommand, ImportJob>>();
            result = await handler.Handle(command, ct);
        }

        if (result.IsFailure)
        {
            Log.Error("Failed to create import job for {FilePath}: [{Code}] {Description}",
                filePath, result.Error!.Code, result.Error.Description);
            return;
        }

        var jobId = _enqueuer.Enqueue(result.Value!.Id);
        Log.Information("Enqueued import job {ImportJobId} (Hangfire: {HangfireJobId}) for {FilePath}",
            result.Value.Id, jobId, filePath);
    }

    private static async Task<bool> WaitForFileReadyAsync(string filePath, CancellationToken ct)
    {
        const int maxAttempts = 10;
        const int delayMs = 500;
        long lastSize = -1;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (ct.IsCancellationRequested) { return false; }
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                var size = stream.Length;
                if (size > 0 && size == lastSize)
                {
                    return true;
                }
                lastSize = size;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        return false;
    }
}
