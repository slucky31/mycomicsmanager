namespace Application.ComicInfoSearch;

/// <summary>
/// Pauses the Bedetheque searches after Cloudflare blocked one, for every request of the application
/// (registered as a singleton): searching again right away would only spend SerpApi calls.
/// </summary>
public sealed class BedethequeCircuit(TimeProvider clock)
{
    private long _pausedUntilUtcTicks;

    public DateTimeOffset? PausedUntil
    {
        get
        {
            var ticks = Interlocked.Read(ref _pausedUntilUtcTicks);
            return ticks > clock.GetUtcNow().UtcTicks ? new DateTimeOffset(ticks, TimeSpan.Zero) : null;
        }
    }

    public DateTimeOffset Pause(TimeSpan duration)
    {
        var until = clock.GetUtcNow().Add(duration);
        Interlocked.Exchange(ref _pausedUntilUtcTicks, until.UtcTicks);
        return until;
    }
}
