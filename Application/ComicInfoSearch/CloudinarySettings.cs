namespace Application.ComicInfoSearch;

public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    // The folder of the covers of this environment (covers, covers-staging, covers-dev), where its unused images
    // are looked for (#1225).
    public string Folder { get; set; } = "covers";
}
