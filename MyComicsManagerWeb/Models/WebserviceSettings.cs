namespace MyComicsManagerWeb.Models
{
    public class WebserviceSettings : IWebserviceSettings
    {
        public string WebserviceUri { get; set; }

        public string FileUploadDirRootPath { get; set; }

        public string CoversDirRootPath { get; set; }

    }

    public interface IWebserviceSettings
    {
        string WebserviceUri { get; set; }

        string FileUploadDirRootPath { get; set; }

        string CoversDirRootPath { get; set; }
    }
}