using System.Collections.Concurrent;
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
    private readonly IHostApplicationLifetime _lifetime;
    private readonly HashSet<string> _supportedExtensions;
    private PeriodicTimer? _periodicTimer;
    private Task? _pollingTask;
    private readonly ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<FileWatcherService> _logger;

    public FileWatcherService(
        IServiceScopeFactory scopeFactory,
        IImportJobEnqueuer enqueuer,
        IOptions<ImportSettings> settings,
        IHostApplicationLifetime lifetime,
        ILogger<FileWatcherService> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _enqueuer = enqueuer;
        _settings = settings.Value;
        _lifetime = lifetime;
        _supportedExtensions = new HashSet<string>(_settings.SupportedExtensions, StringComparer.OrdinalIgnoreCase);
    }

    internal Task? StartupScanTask { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_settings.ImportDirectory);

        // Defer the initial scan until after the host is fully started so that
        // StartAsync returns immediately and does not block HTTP server startup.
        _lifetime.ApplicationStarted.Register(() =>
        {
            var task = Task.Run(() => RunStartupScanAsync(_lifetime.ApplicationStopping), cancellationToken);
            StartupScanTask = task;
            task.ContinueWith(t => _logger.LogError(t.Exception, "Startup scan failed"),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        });

        _periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
        _pollingTask = PollAsync(_lifetime.ApplicationStopping);

        return Task.CompletedTask;
    }

    private async Task RunStartupScanAsync(CancellationToken ct)
    {
        await EnsureImportDirectoriesExistAsync();
        await ScanDirectoryAsync(_settings.ImportDirectory, ct);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        try
        {
            while (await _periodicTimer!.WaitForNextTickAsync(ct))
            {
                try
                {
                    await ScanDirectoryAsync(_settings.ImportDirectory, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Polling scan failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on host shutdown when _lifetime.ApplicationStopping fires.
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _periodicTimer?.Dispose();
        if (_pollingTask is not null)
        {
            await _pollingTask;
        }
    }

    public void Dispose()
    {
        _periodicTimer?.Dispose();
    }

    private async Task EnsureImportDirectoriesExistAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var libraryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Library, Guid>>();
        var importDirectoryStorage = scope.ServiceProvider.GetRequiredService<IImportDirectoryStorage>();

        var libraries = await libraryRepository.ListAsync();
        foreach (var library in libraries.Where(l => l.BookType == LibraryBookType.Digital))
        {
            var result = importDirectoryStorage.EnsureExists(library.ImportDirectoryName);
            if (result.IsFailure)
            {
                _logger.LogWarning("Failed to ensure import directory for library {LibraryId} ({Name}): {Error}",
                    library.Id, library.Name, result.Error?.Description);
            }
        }
    }

    private const string ErrorsDirectoryName = "errors";

    private async Task ScanDirectoryAsync(string rootDir, CancellationToken ct)
    {
        if (!Directory.Exists(rootDir))
        { return; }

        foreach (var subDir in Directory.GetDirectories(rootDir))
        {
            if (string.Equals(Path.GetFileName(subDir), ErrorsDirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(subDir))
            {
                await ProcessFileAsync(file, ct);
            }
        }
    }

    internal async Task ProcessFileAsync(string filePath, CancellationToken ct)
    {
        if (!_inFlight.TryAdd(filePath, 0))
        {
            _logger.LogDebug("Skipping already in-flight file: {FilePath}", filePath);
            return;
        }

        try
        {
            await ProcessFileInternalAsync(filePath, ct);
        }
        finally
        {
            _inFlight.TryRemove(filePath, out _);
        }
    }

    private async Task ProcessFileInternalAsync(string filePath, CancellationToken ct)
    {
        var extension = Path.GetExtension(filePath);
        if (!_supportedExtensions.Contains(extension))
        {
            _logger.LogDebug("Ignoring unsupported file: {FilePath}", filePath);
            return;
        }

        // Library ID is extracted from the immediate parent directory name.
        // Expected format: {RelativePath}_{LibraryId}, e.g. MYCOMICS_01967e23-...
        // Legacy format (GUID only) is also accepted for backward compatibility.
        var parentDir = Path.GetDirectoryName(filePath) ?? string.Empty;
        var parentName = Path.GetFileName(parentDir);
        if (!TryExtractLibraryId(parentName, out var libraryId))
        {
            _logger.LogDebug("Ignoring file at root or non-library subdirectory: {FilePath}", filePath);
            return;
        }

        if (!await WaitForFileReadyAsync(filePath, ct))
        {
            _logger.LogWarning("File never became ready (still locked): {FilePath}", filePath);
            return;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("File disappeared before processing: {FilePath}", filePath);
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
                _logger.LogWarning("Library {LibraryId} not found for file {FilePath}", libraryId, filePath);
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
            if (result.Error!.Code == ImportJobError.AlreadyQueued.Code)
            {
                // Expected while a previously enqueued job for this file is still being
                // processed (extraction/conversion can take minutes); the watcher will
                // stop rescanning it once the file is moved out of the import directory.
                _logger.LogDebug("Import job already active for {FilePath}, skipping rescan", filePath);
            }
            else
            {
                _logger.LogError("Failed to create import job for {FilePath}: [{Code}] {Description}",
                    filePath, result.Error.Code, result.Error.Description);
            }

            return;
        }

        var jobId = _enqueuer.Enqueue(result.Value!.Id);
        _logger.LogInformation("Enqueued import job {ImportJobId} (Hangfire: {HangfireJobId}) for {FilePath}",
            result.Value.Id, jobId, filePath);
    }

    // Extracts the LibraryId GUID from a directory name.
    // Supports {RelativePath}_{Guid} format (e.g. MYCOMICS_01967e23-...)
    // and legacy plain GUID format for backward compatibility.
    private static bool TryExtractLibraryId(string directoryName, out Guid libraryId)
    {
        var lastUnderscore = directoryName.LastIndexOf('_');
        if (lastUnderscore >= 0 && Guid.TryParse(directoryName[(lastUnderscore + 1)..], out libraryId))
        {
            return true;
        }

        return Guid.TryParse(directoryName, out libraryId);
    }

    private async Task<bool> WaitForFileReadyAsync(string filePath, CancellationToken ct)
    {
        const int maxAttempts = 10;
        const int delayMs = 500;
        long lastSize = -1;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (ct.IsCancellationRequested)
            { return false; }
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
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "File not ready yet on attempt {Attempt}: {FilePath}", attempt, filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "File not accessible yet on attempt {Attempt}: {FilePath}", attempt, filePath);
            }

            await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        return false;
    }
}
