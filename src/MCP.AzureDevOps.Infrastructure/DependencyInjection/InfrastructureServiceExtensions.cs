using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Infrastructure.Configuration;
using MCP.AzureDevOps.Infrastructure.Gateways;
using MCP.AzureDevOps.Infrastructure.Persistence;
using MCP.AzureDevOps.Infrastructure.Repositories;
using MCP.AzureDevOps.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
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
        services.AddScoped<IUpstreamMcpGateway, UpstreamMcpGateway>();

        // ── Almacenamiento de cuentas ─────────────────────────────────────
        var connectionString = configuration.GetConnectionString("AccountsDb");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // Modo base de datos: SQLite (por defecto) o SQL Server
            var provider = configuration.GetValue<string>("DatabaseProvider") ?? "sqlite";

            services.AddDbContext<AccountDbContext>(opts =>
            {
                if (provider.Equals("sqlserver", StringComparison.OrdinalIgnoreCase))
                    opts.UseSqlServer(connectionString);
                else
                    opts.UseSqlite(connectionString);
            });

            services.AddSingleton<IPatEncryptionService, AesPatEncryptionService>();
            services.AddScoped<IAccountRepository, DbAccountRepository>();
        }
        else
        {
            // Modo configuración: PATs leídos de appsettings / variables de entorno (sin cifrado)
            services.AddSingleton<IAccountRepository, ConfigurationAccountRepository>();
        }

        return services;
    }
}
