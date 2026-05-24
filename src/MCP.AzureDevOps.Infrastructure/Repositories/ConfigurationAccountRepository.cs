using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Domain.Entities;
using MCP.AzureDevOps.Domain.Exceptions;
using MCP.AzureDevOps.Domain.ValueObjects;
using MCP.AzureDevOps.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace MCP.AzureDevOps.Infrastructure.Repositories;

/// <summary>
/// Repositorio de cuentas respaldado por la configuración (appsettings / env vars).
/// Solo lectura: los métodos de escritura son no-op o lanzan si se usa sin BBDD.
/// Se mantiene por compatibilidad con instalaciones sin base de datos configurada.
/// </summary>
public sealed class ConfigurationAccountRepository : IAccountRepository
{
    private readonly Dictionary<string, Account> _accounts;

    public ConfigurationAccountRepository(IOptions<McpOptions> options)
    {
        _accounts = options.Value.AccountTokens.ToDictionary(
            kvp => kvp.Key,
            kvp => new Account(
                new AccountId(kvp.Key),
                new PersonalAccessToken(kvp.Value)),
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.TryGetValue(id.Value, out var account) ? account : null);

    public Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Account>>(_accounts.Values.ToList());

    public Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        _accounts[account.Id.Value] = account;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        if (!_accounts.ContainsKey(account.Id.Value))
            throw new AccountNotFoundException(account.Id.Value);
        _accounts[account.Id.Value] = account;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        _accounts.Remove(id.Value);
        return Task.CompletedTask;
    }
}
