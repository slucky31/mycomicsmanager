namespace Domain.Series;

public enum SerieCompletenessStatus
{
    // No volume is missing up to the last one owned, but the number of volumes published is unknown.
    Unknown,
    Complete,
    Incomplete,
    // The One Shot books are not a series.
    NotApplicable
}
