namespace MCP.AzureDevOps.Application.Ports.In;

public interface IForwardToolUseCase
{
    /// <summary>
    /// Resuelve la cuenta, autentica y reenvía la llamada al tool upstream.
    /// </summary>
    Task<ToolExecutionResult> ExecuteAsync(
        ForwardToolRequest request,
        CancellationToken cancellationToken = default);
}
