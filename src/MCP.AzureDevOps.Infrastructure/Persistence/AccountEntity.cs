namespace MCP.AzureDevOps.Infrastructure.Persistence;

/// <summary>
/// Modelo EF Core para la tabla Accounts.
/// El PAT siempre se almacena cifrado; el descifrado ocurre en el repositorio.
/// </summary>
public sealed class AccountEntity
{
    /// <summary>Identificador de la cuenta (PK).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>PAT cifrado con AES-256-GCM.</summary>
    public string EncryptedPat { get; set; } = string.Empty;

    /// <summary>Nombre visible de la cuenta.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>URL de destino específica (null = usar la global de McpOptions).</summary>
    public string? TargetUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
