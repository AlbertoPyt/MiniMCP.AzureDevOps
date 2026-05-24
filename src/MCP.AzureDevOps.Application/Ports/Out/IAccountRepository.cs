using MCP.AzureDevOps.Domain.Entities;
using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Application.Ports.Out;

public interface IAccountRepository
{
    Task<Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task DeleteAsync(AccountId id, CancellationToken cancellationToken = default);
}
