namespace MCP.AzureDevOps.Application.Ports.Out;

public interface IAccountRepository
{
    Task<Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve la proyección de todas las cuentas sin descifrar los PATs.
    /// Usar cuando solo se necesitan metadatos (listados, paneles).
    /// </summary>
    Task<IReadOnlyList<AccountInfo>> GetAllInfoAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task DeleteAsync(AccountId id, CancellationToken cancellationToken = default);
}
