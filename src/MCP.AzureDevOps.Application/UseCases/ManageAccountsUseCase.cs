namespace MCP.AzureDevOps.Application.UseCases;

/// <summary>
/// Gestiona el ciclo de vida de las cuentas.
/// El cifrado del PAT lo delega en el repositorio (capa Infrastructure).
/// </summary>
public sealed class ManageAccountsUseCase(IAccountRepository accounts) : IManageAccountsUseCase
{
    public async Task<AccountInfo> RegisterAsync(
        RegisterAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var id = new AccountId(request.AccountId);

        var existing = await accounts.FindByIdAsync(id, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"La cuenta '{request.AccountId}' ya existe.");

        var account = new Account(
            id,
            new PersonalAccessToken(request.Pat),
            request.DisplayName,
            request.TargetUrl);

        await accounts.AddAsync(account, cancellationToken);

        // Devuelve la info sin volver a consultar la BBDD
        return new AccountInfo(account.Id.Value, account.DisplayName, account.TargetUrl, account.CreatedAt);
    }

    public Task RemoveAsync(string accountId, CancellationToken cancellationToken = default)
        // DeleteAsync ya lanza AccountNotFoundException si no existe
        => accounts.DeleteAsync(new AccountId(accountId), cancellationToken);

    public async Task UpdatePatAsync(
        string accountId,
        string newPat,
        CancellationToken cancellationToken = default)
    {
        var id      = new AccountId(accountId);
        var account = await accounts.FindByIdAsync(id, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        account.UpdatePat(new PersonalAccessToken(newPat));
        await accounts.UpdateAsync(account, cancellationToken);
    }

    // Delegación directa: el repositorio proyecta sin descifrar PATs
    public Task<IReadOnlyList<AccountInfo>> GetAllAsync(CancellationToken cancellationToken = default)
        => accounts.GetAllInfoAsync(cancellationToken);
}
