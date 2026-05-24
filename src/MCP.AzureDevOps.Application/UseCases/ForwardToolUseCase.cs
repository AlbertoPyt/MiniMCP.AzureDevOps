using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Domain.Exceptions;
using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Application.UseCases;

public sealed class ForwardToolUseCase(
    IAccountRepository accounts,
    IUpstreamMcpGateway gateway) : IForwardToolUseCase
{
    public async Task<ToolExecutionResult> ExecuteAsync(
        ForwardToolRequest request,
        CancellationToken cancellationToken = default)
    {
        var accountId = new AccountId(request.AccountId);
        var account = accounts.FindById(accountId)
            ?? throw new AccountNotFoundException(request.AccountId);

        return await gateway.CallToolAsync(
            account.Pat,
            request.ToolName,
            request.Arguments,
            cancellationToken);
    }
}
