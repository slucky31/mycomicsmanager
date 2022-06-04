namespace MyComicsManagerWeb.Settings;

public class DatabaseSettings : IDatabaseSettings
{
    public string Name { get; init; }
    public string ConnectionString { get; init; }
}

public interface IDatabaseSettings
{
    public string Name { get; init; }
    public string ConnectionString { get; init; }
}