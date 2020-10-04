namespace MyComicsManagerWeb.Models
{
    public class WebserviceSettings : IWebserviceSettings
    {
        public string WebserviceUri { get; set; }

        public string FileUploadDirRootPath { get; set; }
        
    }

    public interface IWebserviceSettings
    {
        string WebserviceUri { get; set; }

        string FileUploadDirRootPath { get; set; }
    }
}