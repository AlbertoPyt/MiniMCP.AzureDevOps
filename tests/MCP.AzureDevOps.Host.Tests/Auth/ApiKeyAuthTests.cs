namespace MCP.AzureDevOps.Host.Tests.Auth;

/// <summary>
/// Tests E2E de autenticación por API key.
///
/// Verifica que:
/// — Endpoints sin header X-Api-Key devuelven 401.
/// — Endpoints con API key incorrecta devuelven 401.
/// — Endpoints con la API key correcta son accesibles.
/// — El cuerpo del 401 contiene un mensaje de error legible.
/// </summary>
public class ApiKeyAuthTests(McpTestFactory factory) : IClassFixture<McpTestFactory>
{
    // ── Sin autenticación ─────────────────────────────────────────────────

    [Fact]
    public async Task Accounts_SinApiKey_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Accounts_ConApiKeyErronea_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HeaderName, "clave-incorrecta");

        var response = await client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task McpProxy_SinApiKey_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/mcpproxy/forward", new
        {
            accountId = "any",
            toolName  = "workitems_get",
            arguments = new { }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task McpProxy_ConApiKeyErronea_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HeaderName, "wrong-key");

        var response = await client.PostAsJsonAsync("/api/mcpproxy/forward", new
        {
            accountId = "any",
            toolName  = "workitems_get",
            arguments = new { }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Con autenticación correcta ────────────────────────────────────────

    [Fact]
    public async Task Accounts_ConApiKeyCorrecta_Returns200()
    {
        var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Cuerpo de respuesta 401 ───────────────────────────────────────────

    [Fact]
    public async Task Respuesta401_ContieneErrorLegible()
    {
        var client   = factory.CreateClient();
        var response = await client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace("el cliente necesita saber por qué fue rechazado");
        body.Should().Contain("error",
            because: "la respuesta de error sigue el esquema { error: '...' }");
    }

    [Fact]
    public async Task Respuesta401_ContentTypeEsJson()
    {
        var client   = factory.CreateClient();
        var response = await client.GetAsync("/api/accounts");

        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");
    }
}
