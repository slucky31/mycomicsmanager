namespace Domain.Primitives;

// The current time of the domain methods. The callers pass the TimeProvider injected by the DI
// (FakeTimeProvider in the tests); without one, the system clock is used.
public static class Clock
{
    public static DateTime UtcNow(TimeProvider? timeProvider) =>
        (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
}
