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
        var account = await accounts.FindByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(request.AccountId);

        return await gateway.CallToolAsync(
            account.Pat,
            request.ToolName,
            request.Arguments,
            cancellationToken);
    }
}
