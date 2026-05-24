using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MCP.AzureDevOps.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class McpProxyController(
    IForwardToolUseCase forwardUseCase,
    ILogger<McpProxyController> logger) : ControllerBase
{
    [HttpPost("forward")]
    public async Task<IActionResult> Forward(
        [FromHeader(Name = "X-Account-Id")] string accountId,
        [FromBody] ForwardToolRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            return BadRequest("El header X-Account-Id es obligatorio.");

        try
        {
            var result = await forwardUseCase.ExecuteAsync(
                request with { AccountId = accountId },
                cancellationToken);

            return result.IsError
                ? StatusCode(StatusCodes.Status502BadGateway, result.Content)
                : Ok(result.Content);
        }
        catch (AccountNotFoundException ex)
        {
            logger.LogWarning("Unauthorized access attempt for account: {AccountId}", accountId);
            return Unauthorized(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error forwarding to upstream MCP for account {AccountId}", accountId);
            return StatusCode(StatusCodes.Status502BadGateway, "Error contacting Azure DevOps MCP.");
        }
    }
}
