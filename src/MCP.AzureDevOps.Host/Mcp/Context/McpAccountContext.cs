namespace MCP.AzureDevOps.Host.Mcp.Context;

/// <summary>
/// Scoped: holds the active accountId for an MCP session or REST request.
/// Initialised from <see cref="McpOptions.ActiveAccountId"/> (stdio / MCP HTTP mode)
/// or from the <c>X-Account-Id</c> header (REST API mode).
/// </summary>
public sealed class McpAccountContext : IMcpAccountContext
{
    public string AccountId { get; set; }

    public McpAccountContext(IOptions<McpOptions> options)
    {
        AccountId = options.Value.ActiveAccountId;
    }
}
