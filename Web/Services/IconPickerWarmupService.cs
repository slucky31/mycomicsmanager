using Web.Components.SharedComponents;

namespace Web.Services;

/// <summary>
/// Forces the IconPicker static icon dictionary (built via reflection) to initialize
/// at application startup rather than on the first user request.
/// </summary>
internal sealed class IconPickerWarmupService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        IconPicker.Resolve(string.Empty);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
