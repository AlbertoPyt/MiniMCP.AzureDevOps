namespace MCP.AzureDevOps.Sdk;

/// <summary>
/// High-level client for connecting to the Azure DevOps MCP server.
/// </summary>
public sealed class AzureDevOpsMcpClient : IAsyncDisposable
{
    private readonly McpClient _client;

    private AzureDevOpsMcpClient(McpClient client) => _client = client;

    /// <summary>
    /// Creates a client connected to the MCP server via HTTP (Streamable HTTP transport).
    /// </summary>
    /// <param name="serverUrl">MCP server URL, e.g. http://localhost:5263/mcp</param>
    /// <param name="personalAccessToken">Azure DevOps Personal Access Token</param>
    /// <param name="cancellationToken">Cancellation token</param>
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

    /// <summary>Lists all tools available on the server.</summary>
    public async Task<IReadOnlyList<ToolInfo>> ListToolsAsync(
        CancellationToken cancellationToken = default)
    {
        var tools = await _client.ListToolsAsync(cancellationToken: cancellationToken);
        return tools.Select(t => new ToolInfo(t.Name, t.Description ?? string.Empty)).ToList();
    }

    /// <summary>Calls a tool with the given arguments and returns the content as a string.</summary>
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

/// <summary>Basic information about an MCP tool.</summary>
public sealed record ToolInfo(string Name, string Description);
