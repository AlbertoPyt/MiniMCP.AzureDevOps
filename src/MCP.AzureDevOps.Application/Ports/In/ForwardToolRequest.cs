namespace MCP.AzureDevOps.Application.Ports.In;

public sealed record ForwardToolRequest(
    string AccountId,
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments);
