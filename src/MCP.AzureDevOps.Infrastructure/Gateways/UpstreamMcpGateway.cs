using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Domain.ValueObjects;
using MCP.AzureDevOps.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net.Http.Headers;

namespace MCP.AzureDevOps.Infrastructure.Gateways;

public sealed class UpstreamMcpGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<McpOptions> options,
    ILogger<UpstreamMcpGateway> logger) : IUpstreamMcpGateway
{
    private readonly McpOptions _options = options.Value;

    public async Task<ToolExecutionResult> CallToolAsync(
        PersonalAccessToken pat,
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Calling upstream tool '{Tool}' at {Url}", toolName, _options.TargetUrl);

        await using var client = await CreateMcpClientAsync(pat, cancellationToken);

        var result = await client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);

        var content = string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));

        logger.LogInformation("Tool '{Tool}' responded. IsError={IsError}", toolName, result.IsError);

        return new ToolExecutionResult(result.IsError ?? false, content);
    }

    public async Task<IReadOnlyList<ToolDescriptor>> ListToolsAsync(
        PersonalAccessToken pat,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Listing tools from upstream at {Url}", _options.TargetUrl);

        await using var client = await CreateMcpClientAsync(pat, cancellationToken);

        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        return tools.Select(t => new ToolDescriptor(
            t.Name,
            t.Description ?? string.Empty,
            t.JsonSchema.ToString() ?? "{}",
            IsStatic: false)).ToList();
    }

    private async Task<McpClient> CreateMcpClientAsync(
        PersonalAccessToken pat,
        CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient("UpstreamMcp");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", pat.Value);

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(_options.TargetUrl)
            },
            httpClient);

        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }
}
