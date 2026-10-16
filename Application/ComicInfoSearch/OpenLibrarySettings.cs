namespace Application.ComicInfoSearch;

public sealed class OpenLibrarySettings
{
    public required Uri BaseUrl { get; init; }
    public required Uri CoversBaseUrl { get; init; }

    // Off: the ISBN searches skip this source (it can also be turned off in Settings → Features).
    public bool Enabled { get; init; } = true;
}
