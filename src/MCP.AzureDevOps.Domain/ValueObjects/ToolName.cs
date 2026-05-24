namespace MCP.AzureDevOps.Domain.ValueObjects;

public sealed record ToolName
{
    public string Value { get; }

    public ToolName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ToolName cannot be empty.", nameof(value));
        Value = value.Trim();
    }

    public override string ToString() => Value;
}
