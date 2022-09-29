namespace MyComicsManagerApi.Settings;

public class NotificationSettings : INotificationSettings
{
    public string WebserviceUri { get; init; }
    public string Token { get; init; }
}

public interface INotificationSettings
{
    string WebserviceUri { get; init; }
    public string Token { get; init; }
}