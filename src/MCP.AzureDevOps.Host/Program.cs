
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

            services.AddSingleton<IEnumerable<ToolDescriptor>>(StaticToolDescriptors());

            services.AddScoped<McpAccountContext>();
            services.AddScoped<IMcpAccountContext>(sp => sp.GetRequiredService<McpAccountContext>());

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

builder.Services.AddSingleton<IEnumerable<ToolDescriptor>>(StaticToolDescriptors());

builder.Services.AddScoped<McpAccountContext>();
builder.Services.AddScoped<IMcpAccountContext>(sp => sp.GetRequiredService<McpAccountContext>());

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

// ── Autenticación por API key ─────────────────────────────────────────────
builder.Services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();
builder.Services.AddOptions<AuthOptions>()
    .BindConfiguration(AuthOptions.SectionName)
    .ValidateOnStart();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationOptions.Scheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.Scheme, _ => { });

builder.Services.AddAuthorization();

// ── Rate limiting: ventana fija 200 peticiones / minuto por IP ────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var partition = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 200,
            Window               = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit           = 0
        });
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Health checks ─────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── REST API + Swagger ────────────────────────────────────────────────────
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

    // Definición del esquema de seguridad para Swagger UI
    c.AddSecurityDefinition(ApiKeyAuthenticationOptions.Scheme, new OpenApiSecurityScheme
    {
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Header,
        Name        = ApiKeyAuthenticationOptions.HeaderName,
        Description = "API key para acceder a los endpoints críticos. " +
                      "Configura 'Auth:AdminApiKey' en appsettings."
    });

    // Aplicar el esquema globalmente a todos los endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = ApiKeyAuthenticationOptions.Scheme
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await RunMigrationsAndSeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Correlation ID — debe ir antes de cualquier middleware que emita logs
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseRateLimiter();

// El orden importa: Authentication → Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint MCP Streamable HTTP protegido con la misma API key
app.MapMcp("/mcp").RequireAuthorization();

// Health checks — sin autenticación para que los orquestadores puedan sondear
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();

// ── Migraciones automáticas + seed desde configuración ────────────────────
static async Task RunMigrationsAndSeedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var sp = scope.ServiceProvider;

    var dbContext = sp.GetService<AccountDbContext>();
    if (dbContext is null) return;

    await dbContext.Database.MigrateAsync();

    if (!await dbContext.Accounts.AnyAsync())
    {
        var mcpOptions     = sp.GetRequiredService<IOptions<McpOptions>>().Value;
        var manageAccounts = sp.GetRequiredService<IManageAccountsUseCase>();
        var logger         = sp.GetRequiredService<ILogger<Program>>();

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

// ── Descriptores de tools estáticas ──
static IEnumerable<ToolDescriptor> StaticToolDescriptors() =>
[
    new("workitems_get",      "Gets a work item by ID",                     "{}", IsStatic: true),
    new("workitems_create",   "Creates a new work item",                    "{}", IsStatic: true),
    new("workitems_list",     "Queries work items using WIQL",              "{}", IsStatic: true),
    new("pipelines_list",     "Lists pipelines in a project",              "{}", IsStatic: true),
    new("pipelines_run",      "Triggers a pipeline run",                   "{}", IsStatic: true),
    new("repos_list",         "Lists all repositories in a project",       "{}", IsStatic: true),
    new("repos_get_prs",      "Lists pull requests in a repository",       "{}", IsStatic: true),
    new("dynamic_tool_proxy", "Proxy for any other Azure DevOps MCP tool", "{}", IsStatic: true),
];

// Requerido por WebApplicationFactory<Program> en los tests E2E
public partial class Program { }
