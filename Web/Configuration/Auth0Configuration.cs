namespace Web.Configuration;

public class Auth0Configuration : IAuth0Configuration
{
    public string Domain { get; set; } = "DOMAIN";
    public string ClientId { get; set; } = "CLIENTID";
}

public interface IAuth0Configuration
{
    public string Domain { get; set; }
    public string ClientId { get; set; }
}
