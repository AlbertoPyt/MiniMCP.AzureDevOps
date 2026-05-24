namespace MCP.AzureDevOps.Domain.Exceptions;

public sealed class AccountNotFoundException : DomainException
{
    public string AccountId { get; }

    public AccountNotFoundException(string accountId)
        : base($"Account '{accountId}' was not found or is not authorized.")
    {
        AccountId = accountId;
    }
}
