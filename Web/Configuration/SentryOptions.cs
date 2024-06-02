namespace Web.Configuration;

public class SentryOption : ISentryOption
{
    public string Dsn { get; set; } = "DSN";
    public string Environment { get; set; } = "ENVIRONMENT";
}

public interface ISentryOption
{
    public string Dsn { get; set; }
    public string Environment { get; set; }
}
