using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace MCP.AzureDevOps.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IForwardToolUseCase, ForwardToolUseCase>();
        services.AddScoped<IListToolsUseCase, ListToolsUseCase>();
        services.AddScoped<IGetTokenUseCase, GetTokenUseCase>();
        services.AddScoped<IManageAccountsUseCase, ManageAccountsUseCase>();
        return services;
    }
}
