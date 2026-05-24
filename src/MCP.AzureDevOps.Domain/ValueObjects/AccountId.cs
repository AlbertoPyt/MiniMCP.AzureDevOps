namespace MCP.AzureDevOps.Domain.ValueObjects;

public sealed record AccountId
{
    public string Value { get; }

    public AccountId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AccountId cannot be empty.", nameof(value));
        Value = value.Trim();
    }

    public override string ToString() => Value;
}
