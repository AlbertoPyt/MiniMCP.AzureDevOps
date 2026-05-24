namespace MCP.AzureDevOps.Host.Auth;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Authentication scheme name.</summary>
    public const string Scheme = "ApiKey";

    /// <summary>HTTP header name where the key is expected.</summary>
    public const string HeaderName = "X-Api-Key";
}
