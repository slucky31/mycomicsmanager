namespace MyComicsManagerApi.Models
{
    public class DatabaseSettings : IDatabaseSettings
    {
        public string ComicsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IDatabaseSettings
    {
        string ComicsCollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}