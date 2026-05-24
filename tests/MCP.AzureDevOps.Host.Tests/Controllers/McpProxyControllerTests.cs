namespace MCP.AzureDevOps.Host.Tests.Controllers;

/// <summary>
/// Tests E2E de McpProxyController.
///
/// Cadena de integración cubierta:
/// HTTP → McpProxyController → ForwardToolUseCase → ConfigurationAccountRepository
///                                                 → IUpstreamMcpGateway (mock)
///
/// El gateway está mockeado para evitar llamadas reales a Azure DevOps y
/// para poder controlar distintos escenarios (éxito, error upstream, caída HTTP).
/// </summary>
public class McpProxyControllerTests(McpTestFactory factory) : IClassFixture<McpTestFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    // ── Validación del header X-Account-Id ───────────────────────────────

    [Fact]
    public async Task Forward_SinHeaderAccountId_Returns400()
    {
        // El controller exige X-Account-Id; si falta devuelve 400 antes de llamar al use case
        var response = await _client.PostAsJsonAsync("/api/mcpproxy/forward", new
        {
            accountId = "",
            toolName  = "workitems_get",
            arguments = new { }
        });
        // Sin el header, el controller devuelve BadRequest directamente
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Forward_HeaderAccountIdVacio_Returns400()
    {
        var request = ForwardRequest("", "workitems_get");
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Cuenta no encontrada ──────────────────────────────────────────────

    [Fact]
    public async Task Forward_CuentaInexistente_Returns401()
    {
        // AccountNotFoundException → controller devuelve 401
        var request = ForwardRequest("cuenta-que-no-existe", "workitems_get");
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Gateway retorna éxito ─────────────────────────────────────────────

    [Fact]
    public async Task Forward_CuentaValida_GatewayExitoso_Returns200ConContenido()
    {
        // Arrange
        var id = NewId();
        await _client.PostAsJsonAsync("/api/accounts", new { accountId = id, pat = "tok" });

        factory.Gateway
            .CallToolAsync(
                Arg.Any<PersonalAccessToken>(),
                "workitems_get",
                Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ToolExecutionResult(IsError: false, Content: "Work item #42 fetched"));

        // Act
        var response = await _client.SendAsync(ForwardRequest(id, "workitems_get",
            new { id = 42 }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Work item #42 fetched");
    }

    [Fact]
    public async Task Forward_CuentaValida_GatewayRetornaError_Returns502()
    {
        var id = NewId();
        await _client.PostAsJsonAsync("/api/accounts", new { accountId = id, pat = "tok" });

        factory.Gateway
            .CallToolAsync(
                Arg.Any<PersonalAccessToken>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ToolExecutionResult(IsError: true, Content: "Azure DevOps returned 503"));

        var response = await _client.SendAsync(ForwardRequest(id, "pipelines_run"));

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Forward_CuentaValida_GatewayLanzaHttpException_Returns502()
    {
        var id = NewId();
        await _client.PostAsJsonAsync("/api/accounts", new { accountId = id, pat = "tok" });

        factory.Gateway
            .CallToolAsync(
                Arg.Any<PersonalAccessToken>(),
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ToolExecutionResult>(
                new HttpRequestException("Connection refused")));

        var response = await _client.SendAsync(ForwardRequest(id, "repos_list"));

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    // ── El PAT nunca se filtra en respuestas de error ─────────────────────

    [Fact]
    public async Task Forward_CuentaInexistente_RespuestaNoContieneTokenes()
    {
        var request  = ForwardRequest("cuenta-fake", "workitems_get");
        var response = await _client.SendAsync(request);

        // El cuerpo de un 401 es: {"error":"..."} — no debe filtrar tokens ni stack traces
        var body = await response.Content.ReadAsStringAsync();
        var bodyLower = body.ToLowerInvariant();
        bodyLower.Should().NotContain("encryptedpat",
            because: "los PATs cifrados nunca deben exponerse al cliente");
        bodyLower.Should().NotContain("stacktrace",
            because: "los detalles de la pila no deben llegar al cliente");
    }

    // ── Diferentes tools ─────────────────────────────────────────────────

    [Theory]
    [InlineData("workitems_get")]
    [InlineData("workitems_create")]
    [InlineData("pipelines_run")]
    [InlineData("repos_list")]
    public async Task Forward_MultiplesTools_Enrutan_Correctamente(string toolName)
    {
        var id = NewId();
        await _client.PostAsJsonAsync("/api/accounts", new { accountId = id, pat = "tok" });

        factory.Gateway
            .CallToolAsync(
                Arg.Any<PersonalAccessToken>(),
                toolName,
                Arg.Any<IReadOnlyDictionary<string, object?>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ToolExecutionResult(false, $"Result of {toolName}"));

        var response = await _client.SendAsync(ForwardRequest(id, toolName));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string NewId() => $"prx-{Guid.NewGuid():N}";

    /// <summary>
    /// Construye un HttpRequestMessage para POST /api/mcpproxy/forward con el
    /// header X-Account-Id y el cuerpo JSON mínimo.
    /// </summary>
    private static HttpRequestMessage ForwardRequest(
        string accountId,
        string toolName,
        object? arguments = null)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, "/api/mcpproxy/forward");
        if (!string.IsNullOrEmpty(accountId))
            msg.Headers.Add("X-Account-Id", accountId);

        msg.Content = JsonContent.Create(new
        {
            accountId = "",           // siempre se sobreescribe con el header en el controller
            toolName,
            arguments = arguments ?? new { }
        });
        return msg;
    }
}
