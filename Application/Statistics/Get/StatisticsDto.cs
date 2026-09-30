namespace Application.Statistics.Get;

public sealed record StatisticsDto
{
    public int TotalBooks { get; init; }
    public int TotalSeries { get; init; }
    public int TotalLibraries { get; init; }
    public int ReadBooks { get; init; }
    public int UnreadBooks => TotalBooks - ReadBooks;
    public int TotalReadings { get; init; }
    public double? AverageRating { get; init; }
    public long TotalPages { get; init; }
    public long DigitalStorageBytes { get; init; }
    public IReadOnlyList<MonthlyReadingCountDto> ReadingsPerMonth { get; init; } = [];
}
