namespace MCP.AzureDevOps.Application.Ports.In;

/// <summary>
/// CRUD de cuentas (registro, rotación de PAT, eliminación).
/// El cifrado del PAT es responsabilidad de la capa de Infrastructure.
/// </summary>
public interface IManageAccountsUseCase
{
    /// <summary>Registra una nueva cuenta con su PAT en texto plano y devuelve su proyección.</summary>
    Task<AccountInfo> RegisterAsync(RegisterAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>Elimina una cuenta por su identificador.</summary>
    Task RemoveAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>Rota el PAT de una cuenta existente.</summary>
    Task UpdatePatAsync(string accountId, string newPat, CancellationToken cancellationToken = default);

    /// <summary>Devuelve todas las cuentas registradas (sin exponer el PAT).</summary>
    Task<IReadOnlyList<AccountInfo>> GetAllAsync(CancellationToken cancellationToken = default);
}
