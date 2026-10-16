namespace Application.ComicInfoSearch;

public sealed class BnfCatalogueSettings
{
    public required Uri BaseUrl { get; init; }

    // Off: the ISBN searches skip this source (it can also be turned off in Settings → Features).
    public bool Enabled { get; init; } = true;
}
