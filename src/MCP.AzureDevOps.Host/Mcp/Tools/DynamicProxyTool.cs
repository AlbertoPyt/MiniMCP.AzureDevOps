namespace MCP.AzureDevOps.Host.Mcp.Tools;

/// <summary>
/// Dynamic proxy: forwards any tool not registered statically to the upstream MCP transparently.
/// </summary>
[McpServerToolType]
public sealed class DynamicProxyTool(
    IForwardToolUseCase forwardUseCase,
    IMcpAccountContext accountContext)
{
    [McpServerTool(Name = "dynamic_tool_proxy")]
    [Description(
        "Proxy for any Azure DevOps MCP tool not natively defined. " +
        "Use this when the specific tool name is not available as a dedicated tool.")]
    public async Task<string> ProxyAsync(
        [Description("The exact tool name to call on the upstream Azure DevOps MCP")] string toolName,
        [Description("Tool arguments as a JSON object string, e.g. '{\"project\":\"MyProject\"}'")]
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        var args = string.IsNullOrWhiteSpace(argumentsJson)
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson)
              ?? new Dictionary<string, object?>();

        var result = await forwardUseCase.ExecuteAsync(
            new ForwardToolRequest(accountContext.AccountId, toolName, args),
            cancellationToken);

        if (result.IsError) throw new McpException(result.Content);
        return result.Content;
    }
}
