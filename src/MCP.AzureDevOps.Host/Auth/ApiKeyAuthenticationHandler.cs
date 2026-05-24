using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace MCP.AzureDevOps.Host.Auth;

/// <summary>
/// Handler de autenticación por API key.
/// Lee el header <c>X-Api-Key</c> y lo compara con <see cref="AuthOptions.AdminApiKey"/>
/// usando <see cref="CryptographicOperations.FixedTimeEquals"/> (sin comparación de longitud
/// previa que filtraría información sobre la clave configurada).
///
/// Los bytes de la clave se precalculan en el constructor para no asignar memoria en cada petición.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ILogger<ApiKeyAuthenticationHandler> _log;

    // Precalculado en el constructor: null si la clave no está configurada
    private readonly byte[]? _configuredKeyBytes;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<AuthOptions> authOptions)
        : base(options, logger, encoder)
    {
        _log = logger.CreateLogger<ApiKeyAuthenticationHandler>();

        var key = authOptions.Value.AdminApiKey;
        _configuredKeyBytes = string.IsNullOrWhiteSpace(key)
            ? null
            : Encoding.UTF8.GetBytes(key);
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_configuredKeyBytes is null)
        {
            _log.LogWarning(
                "Auth:AdminApiKey no está configurado. Los endpoints críticos están ABIERTOS. " +
                "Configure la clave en producción.");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var headerValues)
            || string.IsNullOrWhiteSpace(headerValues))
        {
            return Task.FromResult(
                AuthenticateResult.Fail($"Header '{ApiKeyAuthenticationOptions.HeaderName}' ausente o vacío."));
        }

        var providedBytes = Encoding.UTF8.GetBytes(headerValues.ToString());

        // FixedTimeEquals ya maneja longitudes distintas correctamente sin revelar la longitud
        // de la clave configurada mediante un código más rápido
        if (!CryptographicOperations.FixedTimeEquals(_configuredKeyBytes, providedBytes))
        {
            _log.LogWarning(
                "Intento de acceso con API key inválida desde {RemoteIp}",
                Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("API key inválida."));
        }

        var claims    = new[] { new Claim(ClaimTypes.Name, "api-client") };
        var identity  = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode  = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        var body = $"{{\"error\":\"Autenticación requerida. Incluye el header '{ApiKeyAuthenticationOptions.HeaderName}' con la API key.\"}}";
        return Response.WriteAsync(body);
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode  = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";
        return Response.WriteAsync("{\"error\":\"Acceso denegado.\"}");
    }
}
