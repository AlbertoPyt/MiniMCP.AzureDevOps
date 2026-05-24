using Microsoft.Extensions.Options;

namespace MCP.AzureDevOps.Host.Auth;

/// <summary>
/// Valida <see cref="AuthOptions"/> en el arranque de la aplicación.
/// Una clave vacía se permite (modo desarrollo sin autenticación), pero si
/// está configurada debe tener al menos <see cref="MinKeyLength"/> caracteres
/// para evitar claves trivialmente débiles.
/// </summary>
internal sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    private const int MinKeyLength = 16;

    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        // Clave vacía: permitido (handler emite advertencia en tiempo de ejecución)
        if (string.IsNullOrWhiteSpace(options.AdminApiKey))
            return ValidateOptionsResult.Success;

        if (options.AdminApiKey.Length < MinKeyLength)
            return ValidateOptionsResult.Fail(
                $"'Auth:AdminApiKey' debe tener al menos {MinKeyLength} caracteres para " +
                "evitar claves débiles. Genera una clave con: openssl rand -base64 32");

        return ValidateOptionsResult.Success;
    }
}
