using Domain.Primitives;

namespace Domain.Series;

public static class SeriesError
{
    public static readonly TError BadRequest = new("SER400", "Verify the request parameters.");
    public static readonly TError InvalidTotalVolumes = new("SER401", $"The number of volumes must be between 1 and {SeriesConstants.MaxTotalVolumes}.");
    public static readonly TError NotFound = new("SER404", "Series not found");
}
