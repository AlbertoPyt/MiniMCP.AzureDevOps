namespace MCP.AzureDevOps.Application.Ports.In;

/// <summary>
/// Proyección de una cuenta para lectura (nunca expone el PAT).
/// </summary>
public sealed record AccountInfo(
    string AccountId,
    string DisplayName,
    string? TargetUrl,
    DateTimeOffset CreatedAt);
