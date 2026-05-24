using Microsoft.AspNetCore.Authentication;

namespace MCP.AzureDevOps.Host.Auth;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Nombre del esquema de autenticación.</summary>
    public const string Scheme = "ApiKey";

    /// <summary>Nombre del header HTTP donde se espera la clave.</summary>
    public const string HeaderName = "X-Api-Key";
}
