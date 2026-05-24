namespace MCP.AzureDevOps.Application.Ports.In;

/// <summary>Datos para registrar una nueva cuenta con su PAT en texto plano.</summary>
public sealed record RegisterAccountRequest(
    string AccountId,
    string Pat,
    string? DisplayName = null,
    string? TargetUrl = null);
