namespace MCP.AzureDevOps.Domain.Entities;

public sealed class Account
{
    public AccountId Id { get; }
    public PersonalAccessToken Pat { get; private set; }
    public string DisplayName { get; private set; }

    /// <summary>
    /// URL de destino específica para esta cuenta. Si es null, se usa el valor global de McpOptions.TargetUrl.
    /// </summary>
    public string? TargetUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public Account(
        AccountId id,
        PersonalAccessToken pat,
        string? displayName = null,
        string? targetUrl = null,
        DateTimeOffset? createdAt = null)
    {
        Id = id;
        Pat = pat;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? id.Value : displayName.Trim();
        TargetUrl = string.IsNullOrWhiteSpace(targetUrl) ? null : targetUrl.Trim();
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    /// <summary>Rota el PAT de la cuenta.</summary>
    public void UpdatePat(PersonalAccessToken newPat) =>
        Pat = newPat ?? throw new ArgumentNullException(nameof(newPat));

    /// <summary>Actualiza el nombre visible de la cuenta.</summary>
    public void UpdateDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Display name cannot be empty.", nameof(name));
        DisplayName = name.Trim();
    }
}
