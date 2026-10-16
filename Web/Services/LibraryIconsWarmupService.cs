using Web.Models;

namespace Web.Services;

/// <summary>
/// Forces the static icon dictionary of LibraryIcons (built via reflection) to initialize
/// at application startup rather than on the first user request.
/// </summary>
internal sealed class LibraryIconsWarmupService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        LibraryIcons.Resolve(string.Empty);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
