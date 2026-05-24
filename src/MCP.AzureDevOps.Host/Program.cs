using MCP.AzureDevOps.Application.DependencyInjection;
using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Domain.Entities;
using MCP.AzureDevOps.Domain.ValueObjects;
using MCP.AzureDevOps.Host.Mcp.Context;
using MCP.AzureDevOps.Host.Mcp.Tools;
using MCP.AzureDevOps.Infrastructure.Configuration;
using MCP.AzureDevOps.Infrastructure.DependencyInjection;
using MCP.AzureDevOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var isStdioMode = args.Contains("--stdio");

if (isStdioMode)
{
    // ── MODO STDIO (Claude Desktop, VS Code local, Visual Studio local) ──
    // Los logs van a stderr para no contaminar el canal stdio del protocolo MCP
    var stdioHost = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
        })
        .ConfigureServices((ctx, services) =>
        {
            services.AddInfrastructureServices(ctx.Configuration);
            services.AddApplicationServices();

            // Tools estáticas inyectadas en ListToolsUseCase
            services.AddSingleton<IEnumerable<ToolDescriptor>>(StaticToolDescriptors());

            // Contexto de cuenta: lee de McpOptions.ActiveAccountId
            services.AddScoped<McpAccountContext>();
            services.AddScoped<IMcpAccountContext>(sp => sp.GetRequiredService<McpAccountContext>());

            // Servidor MCP con transporte stdio
            services.AddMcpServer(opts =>
                {
                    opts.ServerInfo = new() { Name = "AzureDevOps-MCP", Version = "1.0.0" };
                })
                .WithTools<WorkItemTools>()
                .WithTools<PipelinesTools>()
                .WithTools<RepositoriesTools>()
                .WithTools<DynamicProxyTool>()
                .WithStdioServerTransport();
        })
        .Build();

    await RunMigrationsAndSeedAsync(stdioHost.Services);
    await stdioHost.RunAsync();
    return;
}

// ── MODO HTTP (REST API + MCP Streamable HTTP) ──
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

// Tools estáticas para ListToolsUseCase
builder.Services.AddSingleton<IEnumerable<ToolDescriptor>>(StaticToolDescriptors());

// Contexto de cuenta scoped (se puede sobrescribir por middleware para multi-cuenta)
builder.Services.AddScoped<McpAccountContext>();
builder.Services.AddScoped<IMcpAccountContext>(sp => sp.GetRequiredService<McpAccountContext>());

// Servidor MCP con transporte HTTP (Streamable HTTP)
builder.Services
    .AddMcpServer(opts =>
    {
        opts.ServerInfo = new() { Name = "AzureDevOps-MCP", Version = "1.0.0" };
    })
    .WithTools<WorkItemTools>()
    .WithTools<PipelinesTools>()
    .WithTools<RepositoriesTools>()
    .WithTools<DynamicProxyTool>()
    .WithHttpTransport();

// REST API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "AzureDevOps MCP Proxy",
        Version     = "v1",
        Description = "Proxy multi-tenant para el MCP oficial de Azure DevOps"
    });
});

var app = builder.Build();

// Migraciones y seed antes de aceptar tráfico
await RunMigrationsAndSeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Endpoint MCP Streamable HTTP — compatible con VS Code, Copilot, Visual Studio 2026
app.MapMcp("/mcp");

app.Run();

// ── Migraciones automáticas + seed desde configuración ────────────────────
static async Task RunMigrationsAndSeedAsync(IServiceProvider services)
{
    // Solo actuar si el DbContext está registrado (modo base de datos activo)
    var dbContextFactory = services.GetService<IServiceScopeFactory>();
    if (dbContextFactory is null) return;

    using var scope = services.CreateScope();
    var sp = scope.ServiceProvider;

    var dbContext = sp.GetService<AccountDbContext>();
    if (dbContext is null) return;   // modo config-only, nada que migrar

    // Aplicar migraciones pendientes (crea la BD si no existe)
    await dbContext.Database.MigrateAsync();

    // Seed: si la tabla está vacía y hay cuentas en la configuración, las importa
    if (!await dbContext.Accounts.AnyAsync())
    {
        var mcpOptions  = sp.GetRequiredService<IOptions<McpOptions>>().Value;
        var manageAccounts = sp.GetRequiredService<IManageAccountsUseCase>();
        var logger      = sp.GetRequiredService<ILogger<Program>>();

        foreach (var kvp in mcpOptions.AccountTokens)
        {
            await manageAccounts.RegisterAsync(
                new RegisterAccountRequest(AccountId: kvp.Key, Pat: kvp.Value));
        }

        if (mcpOptions.AccountTokens.Count > 0)
            logger.LogInformation(
                "Importadas {Count} cuentas desde la configuración a la base de datos.",
                mcpOptions.AccountTokens.Count);
    }
}

// ── Descriptores de tools estáticas (para ListToolsUseCase) ──
static IEnumerable<ToolDescriptor> StaticToolDescriptors() =>
[
    new("workitems_get",      "Gets a work item by ID",                          "{}", IsStatic: true),
    new("workitems_create",   "Creates a new work item",                         "{}", IsStatic: true),
    new("workitems_list",     "Queries work items using WIQL",                   "{}", IsStatic: true),
    new("pipelines_list",     "Lists pipelines in a project",                    "{}", IsStatic: true),
    new("pipelines_run",      "Triggers a pipeline run",                         "{}", IsStatic: true),
    new("repos_list",         "Lists all repositories in a project",             "{}", IsStatic: true),
    new("repos_get_prs",      "Lists pull requests in a repository",             "{}", IsStatic: true),
    new("dynamic_tool_proxy", "Proxy for any other Azure DevOps MCP tool",       "{}", IsStatic: true),
];
