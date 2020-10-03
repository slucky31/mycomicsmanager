namespace MyComicsManagerWeb.Models
{
    public class WebserviceSettings : IWebserviceSettings
    {
        public string WebserviceUri { get; set; }

        public string LibrariesRootPath { get; set; }
        
    }

    public interface IWebserviceSettings
    {
        string WebserviceUri { get; set; }

        string LibrariesRootPath { get; set; }
    }
}