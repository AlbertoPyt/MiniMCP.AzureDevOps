namespace MCP.AzureDevOps.Domain.Exceptions;

public sealed class UpstreamMcpException : DomainException
{
    public int StatusCode { get; }

    public UpstreamMcpException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
