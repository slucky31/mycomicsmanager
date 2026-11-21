using Domain.Primitives;

namespace Domain.UnitTests.Primitives;

public class ClockTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void UtcNow_Should_ReadTheGivenTimeProvider_InUtc()
    {
        var now = new DateTimeOffset(2026, 11, 21, 18, 30, 0, TimeSpan.FromHours(1));

        var utcNow = Clock.UtcNow(new FixedTimeProvider(now));

        utcNow.Should().Be(new DateTime(2026, 11, 21, 17, 30, 0, DateTimeKind.Utc));
        utcNow.Kind.Should().Be(DateTimeKind.Utc);
    }
}
