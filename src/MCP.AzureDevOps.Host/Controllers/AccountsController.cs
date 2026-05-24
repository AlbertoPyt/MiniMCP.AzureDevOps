using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Domain.Exceptions;
using MCP.AzureDevOps.Host.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace MCP.AzureDevOps.Host.Controllers;

/// <summary>
/// CRUD de cuentas con PAT cifrado.
/// Los PATs nunca se devuelven en las respuestas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AccountsController(IManageAccountsUseCase manageAccounts) : ControllerBase
{
    /// <summary>Devuelve todas las cuentas registradas (sin exponer el PAT).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountInfo>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var accounts = await manageAccounts.GetAllAsync(ct);
        return Ok(accounts);
    }

    /// <summary>Registra una nueva cuenta con su PAT. El PAT se cifra antes de persistir.</summary>
    [HttpPost]
    [ProducesResponseType<AccountInfo>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterAccountDto dto, CancellationToken ct)
    {
        try
        {
            await manageAccounts.RegisterAsync(
                new RegisterAccountRequest(dto.AccountId, dto.Pat, dto.DisplayName, dto.TargetUrl),
                ct);

            // Devolver la cuenta recién creada (sin el PAT)
            var accounts = await manageAccounts.GetAllAsync(ct);
            var created  = accounts.FirstOrDefault(a => a.AccountId == dto.AccountId);

            return CreatedAtAction(nameof(GetAll), created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Rota el PAT de una cuenta existente.</summary>
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
            return NotFound(new { error = $"Cuenta '{accountId}' no encontrada." });
        }
    }

    /// <summary>Elimina una cuenta y su PAT.</summary>
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
            return NotFound(new { error = $"Cuenta '{accountId}' no encontrada." });
        }
    }
}
