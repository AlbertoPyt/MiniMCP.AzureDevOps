namespace MCP.AzureDevOps.Host.Tests.Health;

/// <summary>
/// Tests E2E de los endpoints de health check.
///
/// Los endpoints /health/live y /health/ready no requieren autenticación
/// para que los orquestadores (Kubernetes, Azure App Service, etc.) puedan
/// sondearlos sin credenciales. Se verifica que:
/// - Devuelven 200 OK sin API key.
/// - La respuesta confirma el estado "Healthy".
/// - El header X-Correlation-Id está presente en la respuesta.
/// </summary>
public class HealthCheckTests(McpTestFactory factory) : IClassFixture<McpTestFactory>
{
    // Cliente sin API key — los health checks son públicos
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task HealthLive_SinAutenticacion_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_SinAutenticacion_Returns200()
    {
        var response = await _client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthLive_RespuestaContieneHealthy()
    {
        var body = await _client.GetStringAsync("/health/live");
        body.Should().Be("Healthy", because: "el servidor debería estar sano en tests");
    }

    [Fact]
    public async Task HealthReady_RespuestaContieneHealthy()
    {
        var body = await _client.GetStringAsync("/health/ready");
        body.Should().Be("Healthy", because: "el servidor debería estar sano en tests");
    }

    [Fact]
    public async Task HealthLive_ResponseContieneCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/health/live");
        response.Headers.Contains("X-Correlation-Id").Should().BeTrue(
            because: "el middleware de correlation ID debe añadir el header a todas las respuestas");
    }
}
