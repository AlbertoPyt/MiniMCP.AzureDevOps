namespace MCP.AzureDevOps.Application.Ports.In;

public sealed record ToolExecutionResult(bool IsError, string Content);
