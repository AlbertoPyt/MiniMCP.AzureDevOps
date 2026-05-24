using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Infrastructure.Configuration;
using MCP.AzureDevOps.Infrastructure.Gateways;
using MCP.AzureDevOps.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MCP.AzureDevOps.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<McpOptions>(configuration.GetSection(McpOptions.SectionName));

        services.AddHttpClient("UpstreamMcp");

        services.AddSingleton<IAccountRepository, ConfigurationAccountRepository>();
        services.AddScoped<IUpstreamMcpGateway, UpstreamMcpGateway>();

        return services;
    }
}
