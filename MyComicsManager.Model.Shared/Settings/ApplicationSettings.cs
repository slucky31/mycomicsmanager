namespace MyComicsManager.Model.Shared.Settings
{
    public class ApplicationSettings : IApplicationSettings
    {
        public string ApplicationRootPath { get; set; }
        public string EnvironmentName { get; set; }
        public string CloudinaryName { get; set; }
        public string CloudinaryApiKey { get; set; }
        public string CloudinaryApiSecret { get; set; }
    }

    public interface IApplicationSettings
    {
        public string ApplicationRootPath { get; set; }
        public string EnvironmentName { get; set; }
        public string CloudinaryName { get; set; }
        public string CloudinaryApiKey { get; set; }
        public string CloudinaryApiSecret { get; set; }
    }
}

