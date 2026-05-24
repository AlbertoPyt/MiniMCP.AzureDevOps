using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Application.Ports.Out;

public interface IUpstreamMcpGateway
{
    /// <summary>Llama a un tool en el MCP oficial de Azure DevOps.</summary>
    Task<ToolExecutionResult> CallToolAsync(
        PersonalAccessToken pat,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene la lista de tools disponibles en el upstream.</summary>
    Task<IReadOnlyList<ToolDescriptor>> ListToolsAsync(
        PersonalAccessToken pat,
        CancellationToken cancellationToken = default);
}
