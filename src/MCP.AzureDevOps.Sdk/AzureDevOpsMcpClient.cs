using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net.Http.Headers;

namespace MCP.AzureDevOps.Sdk;

/// <summary>
/// Cliente de alto nivel para conectarse al servidor MCP de Azure DevOps.
/// </summary>
public sealed class AzureDevOpsMcpClient : IAsyncDisposable
{
    private readonly McpClient _client;

    private AzureDevOpsMcpClient(McpClient client) => _client = client;

    /// <summary>
    /// Crea un cliente conectado al servidor MCP vía HTTP (Streamable HTTP transport).
    /// </summary>
    /// <param name="serverUrl">URL del servidor MCP, p.ej. http://localhost:5263/mcp</param>
    /// <param name="personalAccessToken">PAT de Azure DevOps</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    public static async Task<AzureDevOpsMcpClient> CreateAsync(
        Uri serverUrl,
        string personalAccessToken,
        CancellationToken cancellationToken = default)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", personalAccessToken);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = serverUrl },
            httpClient);

        var mcpClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        return new AzureDevOpsMcpClient(mcpClient);
    }

    /// <summary>Lista todos los tools disponibles en el servidor.</summary>
    public async Task<IReadOnlyList<ToolInfo>> ListToolsAsync(
        CancellationToken cancellationToken = default)
    {
        var tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);
        return tools.Select(t => new ToolInfo(t.Name, t.Description ?? string.Empty)).ToList();
    }

    /// <summary>Llama a un tool con los argumentos indicados y devuelve el contenido como string.</summary>
    public async Task<string> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        var result = await _client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);
        return string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await _client.DisposeAsync();
}

/// <summary>Información básica de un tool MCP.</summary>
public sealed record ToolInfo(string Name, string Description);
