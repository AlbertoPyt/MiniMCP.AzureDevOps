using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MCP.AzureDevOps.Host.Tests.Infrastructure;

/// <summary>
/// Fábrica de tests E2E para el Host.
///
/// Estrategia de aislamiento:
/// — Sin conexión a base de datos real (no se provee ConnectionStrings:AccountsDb).
///   Esto activa el ConfigurationAccountRepository (ConcurrentDictionary en memoria),
///   lo que permite hacer CRUD completo de cuentas sin EF Core ni cifrado.
/// — IUpstreamMcpGateway reemplazado por un mock de NSubstitute para evitar
///   llamadas reales a Azure DevOps.
/// — API key de test inyectada en configuración.
///
/// Cada clase de test que use IClassFixture‹McpTestFactory› recibe su propia
/// instancia (y por tanto su propio repositorio en memoria), garantizando
/// aislamiento total entre clases.
/// </summary>
public sealed class McpTestFactory : WebApplicationFactory<Program>
{
    /// <summary>API key que se usa en todos los tests autenticados.</summary>
    public const string TestApiKey = "e2e-test-key-at-least-32-chars!";

    /// <summary>
    /// Mock del gateway upstream compartido para toda la clase de test.
    /// Configura con <c>.Returns(...)</c> al principio de cada test que lo necesite.
    /// </summary>
    public IUpstreamMcpGateway Gateway { get; } = Substitute.For<IUpstreamMcpGateway>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Marca el entorno como Testing para evitar middlewares innecesarios
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Sin ConnectionStrings:AccountsDb → usa ConfigurationAccountRepository (in-memory)
                ["Auth:AdminApiKey"]    = TestApiKey,
                ["Mcp:ActiveAccountId"] = "",
                ["Mcp:TargetUrl"]       = "https://fake.upstream.for-tests.invalid",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Reemplaza el gateway real por el mock (sin llamadas HTTP a Azure DevOps)
            services.RemoveAll<IUpstreamMcpGateway>();
            services.AddScoped<IUpstreamMcpGateway>(_ => Gateway);
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un HttpClient con la API key de test pre-configurada en el header.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies     = false,
        });
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HeaderName, TestApiKey);
        return client;
    }
}
