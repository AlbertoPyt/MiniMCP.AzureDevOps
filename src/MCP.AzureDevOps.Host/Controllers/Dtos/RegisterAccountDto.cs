namespace MCP.AzureDevOps.Host.Controllers.Dtos;

public sealed record RegisterAccountDto(
    [Required, MinLength(1)] string AccountId,
    [Required, MinLength(1)] string Pat,
    string? DisplayName = null,
    string? TargetUrl   = null);
