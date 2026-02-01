namespace Web.Configuration;

public sealed class Auth0Configuration
{
    public required string Domain { get; init; }
    public required string ClientId { get; init; }
}

