namespace MCP.AzureDevOps.Application.Ports.In;

public interface IListToolsUseCase
{
    /// <summary>
    /// Devuelve la unión de tools estáticos (definidos en Host) y dinámicos (descubiertos del upstream).
    /// Las estáticas tienen precedencia ante duplicados.
    /// </summary>
    Task<IReadOnlyList<ToolDescriptor>> GetToolsAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
