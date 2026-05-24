using System.Collections.Concurrent;

namespace MCP.AzureDevOps.Infrastructure.Repositories;

/// <summary>
/// Repositorio de cuentas respaldado por la configuración (appsettings / env vars).
/// Se mantiene por compatibilidad con instalaciones sin base de datos configurada.
/// Usa <see cref="ConcurrentDictionary{TKey,TValue}"/> para ser thread-safe como Singleton.
/// </summary>
public sealed class ConfigurationAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<string, Account> _accounts;

    public ConfigurationAccountRepository(IOptions<McpOptions> options)
    {
        var pairs = options.Value.AccountTokens.Select(kvp =>
            KeyValuePair.Create(
                kvp.Key,
                new Account(new AccountId(kvp.Key), new PersonalAccessToken(kvp.Value))));

        _accounts = new ConcurrentDictionary<string, Account>(
            pairs,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_accounts.TryGetValue(id.Value, out var account) ? account : null);

    public Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Account>>(_accounts.Values.ToList());

    public Task<IReadOnlyList<AccountInfo>> GetAllInfoAsync(CancellationToken cancellationToken = default)
    {
        var result = _accounts.Values
            .Select(a => new AccountInfo(a.Id.Value, a.DisplayName, a.TargetUrl, a.CreatedAt))
            .OrderBy(a => a.DisplayName)
            .ToList();
        return Task.FromResult<IReadOnlyList<AccountInfo>>(result);
    }

    public Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        _accounts.TryAdd(account.Id.Value, account);
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
        if (!_accounts.TryRemove(id.Value, out _))
            throw new AccountNotFoundException(id.Value);
        return Task.CompletedTask;
    }
}
