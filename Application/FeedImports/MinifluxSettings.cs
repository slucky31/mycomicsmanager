namespace Application.FeedImports;

public class MinifluxSettings
{
    public Uri? BaseUrl { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string CategoryName { get; set; } = "BD";
}
