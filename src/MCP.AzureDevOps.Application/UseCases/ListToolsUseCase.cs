namespace MCP.AzureDevOps.Application.UseCases;

public sealed class ListToolsUseCase(
    IAccountRepository accounts,
    IUpstreamMcpGateway gateway,
    IEnumerable<ToolDescriptor> staticTools) : IListToolsUseCase
{
    private readonly IReadOnlyList<ToolDescriptor> _staticTools = staticTools.ToList();

    public async Task<IReadOnlyList<ToolDescriptor>> GetToolsAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var id = new AccountId(accountId);
        var account = await accounts.FindByIdAsync(id, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        // Consultar upstream (puede fallar si no está disponible; lo gestionamos graciosamente)
        IReadOnlyList<ToolDescriptor> upstreamTools;
        try
        {
            upstreamTools = await gateway.ListToolsAsync(account.Pat, cancellationToken);
        }
        catch
        {
            // Si el upstream no está disponible, devolver solo las estáticas
            upstreamTools = Array.Empty<ToolDescriptor>();
        }

        // Las estáticas tienen precedencia; añadir dinámicas que no estén ya registradas
        var staticNames = _staticTools
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dynamicOnly = upstreamTools.Where(t => !staticNames.Contains(t.Name));

        return _staticTools.Concat(dynamicOnly).ToList();
    }
}
