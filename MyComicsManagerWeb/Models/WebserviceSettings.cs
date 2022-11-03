namespace MyComicsManagerWeb.Models
{
    public class WebserviceSettings : IWebserviceSettings
    {
        public string WebserviceUri { get; set; }
        public string ApiGoogleKey { get; set; }

    }

    public interface IWebserviceSettings
    {
        string WebserviceUri { get; set; }
        string ApiGoogleKey { get; set; }
    }
}