namespace MCP.AzureDevOps.Application.Ports.In;

/// <summary>
/// Resuelve el PAT de una cuenta. Usado principalmente por el CLI.
/// </summary>
public interface IGetTokenUseCase
{
    string GetToken(string accountId);
}
