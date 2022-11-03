namespace MyComicsManager.Model.Shared.Settings
{
    public class ApplicationSettings : IApplicationSettings
    {
        public string ApplicationRootPath { get; set; }
    }

    public interface IApplicationSettings
    {
        public string ApplicationRootPath { get; set; }
    }
}
