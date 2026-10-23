namespace Domain.Series;

// What is missing in a series: the gaps up to the last volume owned, and the volumes after it
// when the number of volumes published is known (#1205).
public sealed record SerieCompleteness
{
    public static readonly SerieCompleteness NotApplicable = new() { Status = SerieCompletenessStatus.NotApplicable };

    public SerieCompletenessStatus Status { get; private init; }

    // Number of distinct volumes owned (a volume owned twice counts once).
    public int OwnedCount { get; private init; }

    // The volumes the series should have: the number published, or the last one owned when it is unknown or lower.
    public int ExpectedCount { get; private init; }

    public IReadOnlyList<VolumeRange> Missing { get; private init; } = [];

    public int MissingCount => Missing.Sum(r => r.Count);

    public static SerieCompleteness Of(IEnumerable<int> ownedVolumes, int? totalVolumes)
    {
        ArgumentNullException.ThrowIfNull(ownedVolumes);

        // A volume 0 or below is not a place in the series.
        var owned = ownedVolumes.Where(v => v > 0).Distinct().Order().ToList();
        var expected = Math.Max(totalVolumes ?? 0, owned.Count == 0 ? 0 : owned[^1]);
        var missing = MissingRanges(owned, expected);

        SerieCompletenessStatus status;
        if (missing.Count > 0)
        {
            status = SerieCompletenessStatus.Incomplete;
        }
        else
        {
            status = totalVolumes is null ? SerieCompletenessStatus.Unknown : SerieCompletenessStatus.Complete;
        }

        return new SerieCompleteness
        {
            Status = status,
            OwnedCount = owned.Count,
            ExpectedCount = expected,
            Missing = missing
        };
    }

    private static List<VolumeRange> MissingRanges(List<int> sortedOwned, int expected)
    {
        var ranges = new List<VolumeRange>();
        var next = 1;
        foreach (var volume in sortedOwned)
        {
            if (volume > next)
            {
                ranges.Add(new VolumeRange(next, volume - 1));
            }

            next = volume + 1;
        }

        if (next <= expected)
        {
            ranges.Add(new VolumeRange(next, expected));
        }

        return ranges;
    }
}
