namespace MyComicsManagerApi.Settings
{
    public class GoogleSearchSettings : IGoogleSearchSettings
    {
        // https://console.cloud.google.com/apis/credentials
        public string ApiKey { get; set; }
        // https://programmablesearchengine.google.com/controlpanel/all
        public string Cx { get; set; }
    }

    public interface IGoogleSearchSettings
    {
        public string ApiKey { get; set; }
        public string Cx { get; set; }
    }
}
