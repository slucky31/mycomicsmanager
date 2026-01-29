namespace Application.ComicInfoSearch;

public class GoogleSearchSettings : IGoogleSearchSettings
{
    // https://console.cloud.google.com/apis/credentials
    public required string ApiKey { get; set; }
    // https://programmablesearchengine.google.com/controlpanel/all
    public required string Cx { get; set; }
}

public interface IGoogleSearchSettings
{
    string ApiKey { get; set; }
    string Cx { get; set; }
}
