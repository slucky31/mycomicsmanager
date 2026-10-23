namespace Domain.Series;

// Volumes that follow each other, e.g. the missing volumes 16 to 19 of a series.
public sealed record VolumeRange(int First, int Last)
{
    public int Count => Last - First + 1;
}
