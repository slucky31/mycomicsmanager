using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Services;
using Xunit;

namespace Web.Tests.Services;

public sealed class CircuitPauseOnShutdownTests
{
    private readonly CircuitPauseOnShutdown _service = new(NullLogger<CircuitPauseOnShutdown>.Instance);

    [Fact]
    public async Task PauseAllAsync_Should_ReturnAtOnce_WhenNoCircuitIsOpen()
    {
        await _service.PauseAllAsync(TestContext.Current.CancellationToken);

        _service.OpenCount.Should().Be(0);
    }

    [Fact]
    public async Task PauseAllAsync_Should_AskEveryCircuitToPause_AndWaitForThemToClose()
    {
        var asked = new List<string>();
        var closings = new List<Task>();
        foreach (var id in new[] { "a", "b" })
        {
            _service.Track(id, _ =>
            {
                asked.Add(id);
                closings.Add(CloseLaterAsync(id));
                return Task.FromResult(true);
            });
        }

        await _service.PauseAllAsync(TestContext.Current.CancellationToken);
        await Task.WhenAll(closings);

        asked.Should().BeEquivalentTo("a", "b");
        _service.OpenCount.Should().Be(0);
    }

    [Fact]
    public async Task PauseAllAsync_Should_NotWaitForCircuits_WhenTheirPauseIsRejectedOrFails()
    {
        _service.Track("rejected", _ => Task.FromResult(false));
        _service.Track("failed", _ => throw new InvalidOperationException("Not connected"));
        _service.Track("cancelled", _ => throw new OperationCanceledException());

        var pause = _service.PauseAllAsync(TestContext.Current.CancellationToken);
        var finished = await Task.WhenAny(pause, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        finished.Should().BeSameAs(pause);
        _service.OpenCount.Should().Be(3);
    }

    [Fact]
    public async Task PauseAllAsync_Should_StopWaiting_WhenCancelled()
    {
        _service.Track("stuck", _ => Task.FromResult(true));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await _service.PauseAllAsync(cts.Token);

        _service.OpenCount.Should().Be(1);
    }

    [Fact]
    public async Task StopAsync_Should_PauseTheOpenCircuits()
    {
        var paused = false;
        _service.Track("a", _ =>
        {
            paused = true;
            _service.Untrack("a");
            return Task.FromResult(true);
        });

        await _service.StartAsync(TestContext.Current.CancellationToken);
        await _service.StopAsync(TestContext.Current.CancellationToken);

        paused.Should().BeTrue();
        _service.OpenCount.Should().Be(0);
    }

    private async Task CloseLaterAsync(string id)
    {
        await Task.Delay(50, TestContext.Current.CancellationToken);
        _service.Untrack(id);
    }
}
