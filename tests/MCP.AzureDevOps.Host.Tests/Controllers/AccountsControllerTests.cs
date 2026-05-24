namespace MCP.AzureDevOps.Host.Tests.Controllers;

/// <summary>
/// Tests E2E de AccountsController (CRUD completo de cuentas).
///
/// Usa IClassFixture para compartir la misma instancia del servidor de test
/// dentro de la clase; cada test usa IDs basados en Guid para evitar
/// colisiones de estado aunque la base de datos en memoria sea compartida.
///
/// La cadena de integración real cubierta es:
/// HTTP → AccountsController → ManageAccountsUseCase → ConfigurationAccountRepository
/// (sin BD real ni cifrado de PATs; esos detalles se prueban en Infrastructure.Tests)
/// </summary>
public class AccountsControllerTests(McpTestFactory factory) : IClassFixture<McpTestFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    // ── GET /api/accounts ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200_ConLista()
    {
        var response = await _client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountInfo>>();
        accounts.Should().NotBeNull();
    }

    // ── POST /api/accounts ────────────────────────────────────────────────

    [Fact]
    public async Task Register_DtoValido_Returns201_ConAccountInfo()
    {
        var id = NewId();
        var response = await _client.PostAsJsonAsync("/api/accounts", new
        {
            accountId   = id,
            pat         = "mi-token-secreto",
            displayName = "Equipo Alpha"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<AccountInfo>();
        body!.AccountId.Should().Be(id);
        body.DisplayName.Should().Be("Equipo Alpha");
    }

    [Fact]
    public async Task Register_DtoValido_NoExponePat()
    {
        // El PAT nunca debe aparecer en la respuesta
        var id = NewId();
        var response = await _client.PostAsJsonAsync("/api/accounts", new
        {
            accountId = id,
            pat       = "super-secret-pat-value"
        });

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("super-secret-pat-value",
            because: "el PAT no debe exponerse en ninguna respuesta de la API");
    }

    [Fact]
    public async Task Register_IdDuplicado_Returns409Conflict()
    {
        var id  = NewId();
        var dto = new { accountId = id, pat = "p1" };

        await _client.PostAsJsonAsync("/api/accounts", dto);          // primera vez → 201
        var response = await _client.PostAsJsonAsync("/api/accounts", dto);  // duplicado

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_SinPat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new
        {
            accountId = NewId()
            // pat ausente — campo requerido
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_SinAccountId_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new
        {
            pat = "algún-token"
            // accountId ausente — campo requerido
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_PatVacio_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new
        {
            accountId = NewId(),
            pat       = ""  // MinLength(1) → 400
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/accounts/{id}/pat ────────────────────────────────────────

    [Fact]
    public async Task UpdatePat_CuentaExistente_Returns204()
    {
        var id = NewId();
        await _client.PostAsJsonAsync("/api/accounts", new { accountId = id, pat = "old-pat" });

        var response = await _client.PutAsJsonAsync($"/api/accounts/{id}/pat",
            new { pat = "rotated-pat" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdatePat_CuentaInexistente_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/accounts/ghost-acct/pat",
            new { pat = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/accounts/{id} ─────────────────────────────────────────

    [Fact]
    public async Task Delete_CuentaExistente_Returns204()
    {
        var id = NewId();
        await _client.PostAsJsonAsync("/api/accounts", new { accountId = id, pat = "p" });

        var response = await _client.DeleteAsync($"/api/accounts/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_CuentaInexistente_Returns404()
    {
        var response = await _client.DeleteAsync("/api/accounts/cuenta-fantasma-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Flujos completos (estado) ─────────────────────────────────────────

    [Fact]
    public async Task Register_AparecePosteriormenteEnGetAll()
    {
        var id = NewId();
        await _client.PostAsJsonAsync("/api/accounts", new
        {
            accountId   = id,
            pat         = "p",
            displayName = "Mi Cuenta E2E"
        });

        var response  = await _client.GetAsync("/api/accounts");
        var accounts  = await response.Content.ReadFromJsonAsync<List<AccountInfo>>();

        accounts.Should().ContainSingle(
            a => a.AccountId == id && a.DisplayName == "Mi Cuenta E2E",
            because: "la cuenta recién registrada debe ser visible en la lista");
    }

    [Fact]
    public async Task Register_ThenDelete_YaNoAparecEnGetAll()
    {
        var id = NewId();
        await _client.PostAsJsonAsync("/api/accounts", new { accountId = id, pat = "p" });
        await _client.DeleteAsync($"/api/accounts/{id}");

        var response = await _client.GetAsync("/api/accounts");
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountInfo>>();

        accounts.Should().NotContain(a => a.AccountId == id,
            because: "la cuenta eliminada no debe seguir apareciendo");
    }

    [Fact]
    public async Task Register_ThenUpdatePat_ThenDelete_CicloCompleto()
    {
        var id = NewId();

        // Crear
        var create = await _client.PostAsJsonAsync("/api/accounts",
            new { accountId = id, pat = "v1" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        // Rotar PAT
        var update = await _client.PutAsJsonAsync($"/api/accounts/{id}/pat",
            new { pat = "v2" });
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Eliminar
        var delete = await _client.DeleteAsync($"/api/accounts/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar que ya no existe
        var del2 = await _client.DeleteAsync($"/api/accounts/{id}");
        del2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    /// <summary>Genera un ID único por test para evitar colisiones de estado.</summary>
    private static string NewId() => $"acc-{Guid.NewGuid():N}";
}
