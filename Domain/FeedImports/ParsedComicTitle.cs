namespace Domain.FeedImports;

public sealed record ParsedComicTitle(string? Serie, string? Title, int? Volume, string? Isbn = null)
{
    public static ParsedComicTitle Empty { get; } = new(null, null, null);

    public bool HasSerie => !string.IsNullOrWhiteSpace(Serie);
}
