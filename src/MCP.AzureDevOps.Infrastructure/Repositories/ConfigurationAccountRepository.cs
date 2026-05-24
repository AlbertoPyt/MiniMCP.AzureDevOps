using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Domain.Entities;
using MCP.AzureDevOps.Domain.ValueObjects;
using MCP.AzureDevOps.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace MCP.AzureDevOps.Infrastructure.Repositories;

public sealed class ConfigurationAccountRepository : IAccountRepository
{
    private readonly IReadOnlyDictionary<string, Account> _accounts;

    public ConfigurationAccountRepository(IOptions<McpOptions> options)
    {
        _accounts = options.Value.AccountTokens.ToDictionary(
            kvp => kvp.Key,
            kvp => new Account(
                new AccountId(kvp.Key),
                new PersonalAccessToken(kvp.Value)),
            StringComparer.OrdinalIgnoreCase);
    }

    public Account? FindById(AccountId id) =>
        _accounts.TryGetValue(id.Value, out var account) ? account : null;

    public IReadOnlyList<Account> GetAll() =>
        _accounts.Values.ToList();
}
