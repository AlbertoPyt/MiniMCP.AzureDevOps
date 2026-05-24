using MCP.AzureDevOps.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace MCP.AzureDevOps.Host.Mcp.Context;

/// <summary>
/// Scoped: mantiene el accountId activo para una sesión MCP o request REST.
/// Se inicializa desde McpOptions.ActiveAccountId (modo stdio/MCP HTTP)
/// o desde el header X-Account-Id (modo REST API).
/// </summary>
public sealed class McpAccountContext : IMcpAccountContext
{
    public string AccountId { get; set; }

    public McpAccountContext(IOptions<McpOptions> options)
    {
        AccountId = options.Value.ActiveAccountId;
    }
}
