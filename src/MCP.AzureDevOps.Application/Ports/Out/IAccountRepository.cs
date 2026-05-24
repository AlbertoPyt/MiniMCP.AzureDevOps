using MCP.AzureDevOps.Domain.Entities;
using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Application.Ports.Out;

public interface IAccountRepository
{
    Account? FindById(AccountId id);
    IReadOnlyList<Account> GetAll();
}
