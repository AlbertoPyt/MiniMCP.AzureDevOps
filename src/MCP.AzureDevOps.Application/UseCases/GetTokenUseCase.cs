using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Domain.Exceptions;
using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Application.UseCases;

public sealed class GetTokenUseCase(IAccountRepository accounts) : IGetTokenUseCase
{
    public string GetToken(string accountId)
    {
        var id = new AccountId(accountId);
        var account = accounts.FindById(id)
            ?? throw new AccountNotFoundException(accountId);
        return account.Pat.Value;
    }
}
