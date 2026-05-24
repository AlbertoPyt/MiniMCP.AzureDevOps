namespace MCP.AzureDevOps.Domain.Entities;

/// <summary>
/// Representa la definición de un tool MCP (estático o descubierto dinámicamente).
/// </summary>
public sealed class ToolDefinition
{
    public ToolName Name { get; }
    public string Description { get; }
    public string InputSchemaJson { get; }
    public bool IsStatic { get; }

    public ToolDefinition(ToolName name, string description, string inputSchemaJson, bool isStatic)
    {
        Name = name;
        Description = description;
        InputSchemaJson = inputSchemaJson;
        IsStatic = isStatic;
    }
}
