namespace MCP.AzureDevOps.Host.Middleware;

/// <summary>
/// Propaga el header <c>X-Correlation-Id</c> en toda la cadena de petición/respuesta.
/// <list type="bullet">
///   <item>Si la petición incluye el header, lo reutiliza.</item>
///   <item>Si no, genera un nuevo <see cref="Guid"/> en formato sin guiones.</item>
/// </list>
/// El ID se añade al scope del logger para que aparezca en todos los mensajes de
/// la petición actual, y se devuelve en el header de respuesta.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString("N");

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
