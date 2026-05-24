using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Domain.Exceptions;
using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Application.UseCases;

public sealed class GetTokenUseCase(IAccountRepository accounts) : IGetTokenUseCase
{
    public async Task<string> GetTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var id = new AccountId(accountId);
        var account = await accounts.FindByIdAsync(id, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);
        return account.Pat.Value;
    }
}
