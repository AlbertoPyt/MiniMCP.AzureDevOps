namespace MCP.AzureDevOps.Infrastructure.Configuration;

public sealed class McpOptions
{
    public const string SectionName = "Mcp";

    /// <summary>URL del MCP oficial de Azure DevOps.</summary>
    public string TargetUrl { get; init; } = string.Empty;

    /// <summary>
    /// Cuenta activa para el MCP Server (stdio/HTTP).
    /// En modo REST, el accountId llega por header X-Account-Id.
    /// </summary>
    public string ActiveAccountId { get; init; } = string.Empty;

    /// <summary>Mapa de accountId → Personal Access Token.</summary>
    public Dictionary<string, string> AccountTokens { get; init; } = [];
}
