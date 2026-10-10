using Application.ComicInfoSearch;
using NSubstitute;

namespace Application.UnitTests.ComicInfoSearch;

public sealed class BedethequeCircuitTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();

    public BedethequeCircuitTests()
    {
        _clock.GetUtcNow().Returns(s_now);
    }

    [Fact]
    public void PausedUntil_Should_BeNull_WhenTheCircuitWasNeverPaused()
    {
        new BedethequeCircuit(_clock).PausedUntil.Should().BeNull();
    }

    [Fact]
    public void Pause_Should_PauseTheSearchesForTheGivenDuration()
    {
        var circuit = new BedethequeCircuit(_clock);

        var until = circuit.Pause(TimeSpan.FromHours(24));

        until.Should().Be(s_now.AddHours(24));
        circuit.PausedUntil.Should().Be(s_now.AddHours(24));
    }

    [Fact]
    public void PausedUntil_Should_BeNull_WhenThePauseIsOver()
    {
        var circuit = new BedethequeCircuit(_clock);
        circuit.Pause(TimeSpan.FromHours(24));

        _clock.GetUtcNow().Returns(s_now.AddHours(25));

        circuit.PausedUntil.Should().BeNull();
    }
}
