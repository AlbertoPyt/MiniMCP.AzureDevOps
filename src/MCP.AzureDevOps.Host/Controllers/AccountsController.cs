namespace MCP.AzureDevOps.Host.Controllers;

/// <summary>
/// Account CRUD with encrypted PAT storage.
/// PATs are never returned in responses.
/// Requires API key authentication (X-Api-Key header).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AccountsController(IManageAccountsUseCase manageAccounts) : ControllerBase
{
    /// <summary>Returns all registered accounts without exposing the PAT.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountInfo>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var accounts = await manageAccounts.GetAllAsync(ct);
        return Ok(accounts);
    }

    /// <summary>Registers a new account with its PAT. The PAT is encrypted before persisting.</summary>
    [HttpPost]
    [ProducesResponseType<AccountInfo>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterAccountDto dto, CancellationToken ct)
    {
        try
        {
            var created = await manageAccounts.RegisterAsync(
                new RegisterAccountRequest(dto.AccountId, dto.Pat, dto.DisplayName, dto.TargetUrl),
                ct);

            return CreatedAtAction(nameof(GetAll), created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Rotates the PAT for an existing account.</summary>
    [HttpPut("{accountId}/pat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePat(
        string accountId,
        [FromBody] UpdatePatDto dto,
        CancellationToken ct)
    {
        try
        {
            await manageAccounts.UpdatePatAsync(accountId, dto.Pat, ct);
            return NoContent();
        }
        catch (AccountNotFoundException)
        {
            return NotFound(new { error = $"Account '{accountId}' not found." });
        }
    }

    /// <summary>Deletes an account and its PAT.</summary>
    [HttpDelete("{accountId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string accountId, CancellationToken ct)
    {
        try
        {
            await manageAccounts.RemoveAsync(accountId, ct);
            return NoContent();
        }
        catch (AccountNotFoundException)
        {
            return NotFound(new { error = $"Account '{accountId}' not found." });
        }
    }
}
