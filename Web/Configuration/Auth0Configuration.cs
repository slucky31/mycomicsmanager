namespace Web.Configuration;

internal sealed class Auth0Configuration : IAuth0Configuration
{
    public string Domain { get; set; } = "DOMAIN";
    public string ClientId { get; set; } = "CLIENTID";
}

internal interface IAuth0Configuration
{
    public string Domain { get; set; }
    public string ClientId { get; set; }
}
