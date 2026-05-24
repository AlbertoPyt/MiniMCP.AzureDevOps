namespace MCP.AzureDevOps.Host.Auth;

/// <summary>
/// Opciones de autenticación del servidor.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// API key que protege los endpoints críticos (REST + MCP HTTP).
    /// Se envía en el header <c>X-Api-Key</c>.
    /// Si está vacío, la autenticación queda deshabilitada (solo para desarrollo local).
    /// Genera una clave segura con: openssl rand -base64 32
    /// </summary>
    public string AdminApiKey { get; init; } = string.Empty;
}
