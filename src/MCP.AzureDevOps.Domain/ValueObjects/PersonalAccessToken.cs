namespace MCP.AzureDevOps.Domain.ValueObjects;

public sealed record PersonalAccessToken
{
    public string Value { get; }

    public PersonalAccessToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PersonalAccessToken cannot be empty.", nameof(value));
        Value = value.Trim();
    }

    // Deliberadamente sin ToString() para evitar logging accidental del token
}
