namespace MCP.AzureDevOps.Infrastructure.Repositories;

/// <summary>
/// Repositorio de cuentas respaldado por base de datos (SQLite o SQL Server).
/// Cifra el PAT al escribir y lo descifra al leer; el dominio siempre trabaja
/// con PATs en texto plano.
/// </summary>
internal sealed class DbAccountRepository(
    AccountDbContext db,
    IPatEncryptionService encryption) : IAccountRepository
{
    public async Task<Account?> FindByIdAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Accounts.FindAsync([id.Value], cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await db.Accounts
            .OrderBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    /// <summary>
    /// Proyección sin descifrado: la consulta se resuelve en SQL sin traer PATs a memoria.
    /// </summary>
    public async Task<IReadOnlyList<AccountInfo>> GetAllInfoAsync(CancellationToken cancellationToken = default)
        => await db.Accounts
            .OrderBy(a => a.DisplayName)
            .Select(e => new AccountInfo(e.Id, e.DisplayName, e.TargetUrl, e.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new AccountEntity
        {
            Id           = account.Id.Value,
            EncryptedPat = encryption.Encrypt(account.Pat.Value),
            DisplayName  = account.DisplayName,
            TargetUrl    = account.TargetUrl,
            CreatedAt    = now,
            UpdatedAt    = now
        };

        db.Accounts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        var entity = await db.Accounts.FindAsync([account.Id.Value], cancellationToken)
            ?? throw new AccountNotFoundException(account.Id.Value);

        entity.EncryptedPat = encryption.Encrypt(account.Pat.Value);
        entity.DisplayName  = account.DisplayName;
        entity.TargetUrl    = account.TargetUrl;
        entity.UpdatedAt    = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Accounts.FindAsync([id.Value], cancellationToken)
            ?? throw new AccountNotFoundException(id.Value);

        db.Accounts.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Mapeo Entity → Domain ──────────────────────────────────────────────
    private Account ToDomain(AccountEntity entity)
    {
        var plainPat = encryption.Decrypt(entity.EncryptedPat);
        return new Account(
            new AccountId(entity.Id),
            new PersonalAccessToken(plainPat),
            entity.DisplayName,
            entity.TargetUrl,
            entity.CreatedAt);
    }
}
