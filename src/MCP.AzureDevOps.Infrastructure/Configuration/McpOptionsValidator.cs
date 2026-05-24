using Microsoft.Extensions.Options;

namespace MCP.AzureDevOps.Infrastructure.Configuration;

/// <summary>
/// Valida <see cref="McpOptions"/> en el arranque de la aplicación.
/// Fallos detectados aquí abortan el inicio con un mensaje claro en lugar
/// de producir errores crípticos durante la primera petición.
/// </summary>
internal sealed class McpOptionsValidator : IValidateOptions<McpOptions>
{
    public ValidateOptionsResult Validate(string? name, McpOptions options)
    {
        var failures = new List<string>();

        // TargetUrl es necesaria para reenviar herramientas al upstream MCP.
        // Puede estar vacía en un entorno puramente de configuración (sin BD ni upstream),
        // pero si está definida debe ser una URI absoluta válida.
        if (!string.IsNullOrWhiteSpace(options.TargetUrl)
            && !Uri.TryCreate(options.TargetUrl, UriKind.Absolute, out _))
        {
            failures.Add(
                $"'Mcp:TargetUrl' no es una URI absoluta válida: '{options.TargetUrl}'. " +
                "Ejemplo: https://azuredevops.mcpserver.microsoft.com");
        }

        // DbEncryptionKey debe tener exactamente 32 bytes en base64 cuando se usa modo BD.
        // (La validación más profunda la hace AesPatEncryptionService al construirse.)
        if (!string.IsNullOrWhiteSpace(options.DbEncryptionKey))
        {
            try
            {
                var keyBytes = Convert.FromBase64String(options.DbEncryptionKey);
                if (keyBytes.Length != 32)
                    failures.Add(
                        $"'Mcp:DbEncryptionKey' debe ser de 32 bytes en base64 (actual: {keyBytes.Length} bytes). " +
                        "Genera una clave con: openssl rand -base64 32");
            }
            catch (FormatException)
            {
                failures.Add(
                    "'Mcp:DbEncryptionKey' no está correctamente codificado en base64. " +
                    "Genera una clave con: openssl rand -base64 32");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
