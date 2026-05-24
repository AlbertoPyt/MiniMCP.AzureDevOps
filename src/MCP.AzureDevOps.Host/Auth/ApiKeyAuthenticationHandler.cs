using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace MCP.AzureDevOps.Host.Auth;

/// <summary>
/// Handler de autenticación por API key.
/// Lee el header <c>X-Api-Key</c> y lo compara con <see cref="AuthOptions.AdminApiKey"/>
/// usando comparación en tiempo constante (previene timing attacks).
///
/// Comportamiento:
/// - Clave no configurada → NoResult (auth deshabilitada, solo aceptable en desarrollo).
/// - Header ausente       → Fail (401).
/// - Clave incorrecta     → Fail (401).
/// - Clave correcta       → Success (identidad "api-client").
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    private readonly ILogger<ApiKeyAuthenticationHandler> _log =
        logger.CreateLogger<ApiKeyAuthenticationHandler>();

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = authOptions.Value.AdminApiKey;

        // Si no hay clave configurada, la autenticación queda deshabilitada.
        // Se emite una advertencia para que no pase desapercibido en producción.
        if (string.IsNullOrWhiteSpace(configuredKey))
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

        var providedKey  = headerValues.ToString();
        var configBytes  = Encoding.UTF8.GetBytes(configuredKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);

        // Comparación en tiempo constante: evita inferir la clave por timing
        if (configBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(configBytes, providedBytes))
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

    /// <summary>Devuelve 401 con un mensaje claro indicando el header requerido.</summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode  = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        var body = $"{{\"error\":\"Autenticación requerida. Incluye el header '{ApiKeyAuthenticationOptions.HeaderName}' con la API key.\"}}";
        return Response.WriteAsync(body);
    }

    /// <summary>Devuelve 403 si el usuario está autenticado pero no autorizado.</summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode  = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";
        return Response.WriteAsync("{\"error\":\"Acceso denegado.\"}");
    }
}
