using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Domain.Entities;
using MCP.AzureDevOps.Domain.Exceptions;
using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Application.UseCases;

/// <summary>
/// Gestiona el ciclo de vida de las cuentas.
/// El cifrado del PAT lo delega en el repositorio (capa Infrastructure).
/// </summary>
public sealed class ManageAccountsUseCase(IAccountRepository accounts) : IManageAccountsUseCase
{
    public async Task RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken = default)
    {
        var id = new AccountId(request.AccountId);

        var existing = await accounts.FindByIdAsync(id, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"La cuenta '{request.AccountId}' ya existe.");

        // El repositorio cifra el PAT al persistir; aquí el dominio trabaja con texto plano
        var account = new Account(
            id,
            new PersonalAccessToken(request.Pat),
            request.DisplayName,
            request.TargetUrl);

        await accounts.AddAsync(account, cancellationToken);
    }

    public async Task RemoveAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var id = new AccountId(accountId);

        _ = await accounts.FindByIdAsync(id, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        await accounts.DeleteAsync(id, cancellationToken);
    }

    public async Task UpdatePatAsync(string accountId, string newPat, CancellationToken cancellationToken = default)
    {
        var id = new AccountId(accountId);

        var account = await accounts.FindByIdAsync(id, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        // El repositorio cifra el nuevo PAT al persistir
        account.UpdatePat(new PersonalAccessToken(newPat));
        await accounts.UpdateAsync(account, cancellationToken);
    }

    public async Task<IReadOnlyList<AccountInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await accounts.GetAllAsync(cancellationToken);
        return all
            .Select(a => new AccountInfo(a.Id.Value, a.DisplayName, a.TargetUrl, a.CreatedAt))
            .ToList();
    }
}
