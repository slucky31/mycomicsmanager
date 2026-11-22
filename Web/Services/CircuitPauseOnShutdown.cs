using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Web.Services;

/// <summary>
/// Keeps the open circuits and, when the app stops (a deployment restarts the container), asks their
/// browsers to pause them: the state of each page is kept in the browser and the page comes back where it
/// was once the new container is up, instead of the reconnection screen (#1342).
/// </summary>
public sealed class CircuitPauseOnShutdown(ILogger<CircuitPauseOnShutdown> logger) : CircuitHandler, IHostedService
{
    // Below the 10 s Docker gives a container before killing it.
    internal static readonly TimeSpan PauseTimeout = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan s_closeCheckInterval = TimeSpan.FromMilliseconds(100);

    private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<bool>>> _circuits = new(StringComparer.Ordinal);

    internal int OpenCount => _circuits.Count;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Track(circuit.Id, circuit.RequestCircuitPauseAsync);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        Untrack(circuit.Id);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(PauseTimeout);
        await PauseAllAsync(timeout.Token);
    }

    internal void Track(string circuitId, Func<CancellationToken, Task<bool>> requestPause) => _circuits[circuitId] = requestPause;

    internal void Untrack(string circuitId) => _circuits.TryRemove(circuitId, out _);

    /// <summary>Asks every open circuit to pause, then waits for the accepted ones to close (their state is then saved).</summary>
    internal async Task PauseAllAsync(CancellationToken cancellationToken)
    {
        var circuits = _circuits.ToArray();
        if (circuits.Length == 0)
        {
            return;
        }

        logger.LogInformation("Asking {Count} open circuit(s) to pause before the app stops", circuits.Length);
        var accepted = (await Task.WhenAll(circuits.Select(c => RequestPauseAsync(c.Key, c.Value, cancellationToken))))
            .OfType<string>()
            .ToList();

        try
        {
            while (accepted.Exists(_circuits.ContainsKey))
            {
                await Task.Delay(s_closeCheckInterval, cancellationToken);
            }

            logger.LogInformation("{Count} circuit(s) paused before the app stops", accepted.Count);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "{Count} circuit(s) still pausing when the app stops", accepted.Count(_circuits.ContainsKey));
        }
    }

    // The circuit id when the browser accepted to pause, null otherwise.
    private async Task<string?> RequestPauseAsync(string circuitId, Func<CancellationToken, Task<bool>> requestPause, CancellationToken cancellationToken)
    {
        try
        {
            return await requestPause(cancellationToken) ? circuitId : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Circuit {CircuitId} cannot be asked to pause", circuitId);
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
