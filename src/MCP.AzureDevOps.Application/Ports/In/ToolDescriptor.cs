namespace MCP.AzureDevOps.Application.Ports.In;

public sealed record ToolDescriptor(
    string Name,
    string Description,
    string InputSchemaJson,
    bool IsStatic);
