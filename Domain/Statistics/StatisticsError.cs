using Domain.Primitives;

namespace Domain.Statistics;

public static class StatisticsError
{
    public static readonly TError BadRequest = new("STA400", "Verify the request parameters.");
}
