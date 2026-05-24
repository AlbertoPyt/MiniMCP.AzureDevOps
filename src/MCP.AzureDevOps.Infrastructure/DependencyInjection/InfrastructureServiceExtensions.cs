using MCP.AzureDevOps.Infrastructure.Configuration;
using MCP.AzureDevOps.Infrastructure.Gateways;
using MCP.AzureDevOps.Infrastructure.Persistence;
using MCP.AzureDevOps.Infrastructure.Repositories;
using MCP.AzureDevOps.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MCP.AzureDevOps.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuración con validación en el arranque
        services.AddSingleton<IValidateOptions<McpOptions>, McpOptionsValidator>();
        services.AddOptions<McpOptions>()
            .BindConfiguration(McpOptions.SectionName)
            .ValidateOnStart();

        // HttpClient con resiliencia: reintentos + circuit-breaker automáticos (Polly)
        services.AddHttpClient("UpstreamMcp")
            .AddStandardResilienceHandler();

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
