using FluentAssertions;
using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Application.UseCases;
using MCP.AzureDevOps.Domain.Entities;
using MCP.AzureDevOps.Domain.Exceptions;
using MCP.AzureDevOps.Domain.ValueObjects;
using NSubstitute;

namespace MCP.AzureDevOps.Application.Tests.UseCases;

public class ManageAccountsUseCaseTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly ManageAccountsUseCase _sut;

    public ManageAccountsUseCaseTests()
    {
        _sut = new ManageAccountsUseCase(_accounts);
    }

    // ── RegisterAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithNewAccount_AddsToRepository()
    {
        // Arrange: ninguna cuenta existente
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns((Account?)null);

        var request = new RegisterAccountRequest("equipo-a", "mi-pat", "Equipo A");

        // Act
        await _sut.RegisterAsync(request);

        // Assert: AddAsync llamado exactamente una vez con la cuenta correcta
        await _accounts.Received(1).AddAsync(
            Arg.Is<Account>(a => a.Id.Value == "equipo-a" && a.DisplayName == "Equipo A"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateAccount_ThrowsInvalidOperationException()
    {
        // Arrange: la cuenta ya existe
        var existing = new Account(new AccountId("equipo-a"), new PersonalAccessToken("old-pat"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(existing);

        // Act & Assert
        var act = async () => await _sut.RegisterAsync(
            new RegisterAccountRequest("equipo-a", "new-pat"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*equipo-a*");

        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_WithExistingAccount_CallsDelete()
    {
        // Arrange
        var account = new Account(new AccountId("equipo-a"), new PersonalAccessToken("tok"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(account);

        // Act
        await _sut.RemoveAsync("equipo-a");

        // Assert
        await _accounts.Received(1).DeleteAsync(
            Arg.Is<AccountId>(id => id.Value == "equipo-a"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistingAccount_ThrowsAccountNotFoundException()
    {
        // Arrange
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns((Account?)null);

        // Act & Assert
        var act = async () => await _sut.RemoveAsync("no-existe");
        await act.Should().ThrowAsync<AccountNotFoundException>();

        await _accounts.DidNotReceive().DeleteAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    // ── UpdatePatAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePatAsync_WithExistingAccount_UpdatesRepository()
    {
        // Arrange
        var account = new Account(new AccountId("equipo-a"), new PersonalAccessToken("old-pat"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(account);

        // Act
        await _sut.UpdatePatAsync("equipo-a", "new-pat");

        // Assert: UpdateAsync llamado con el nuevo PAT
        await _accounts.Received(1).UpdateAsync(
            Arg.Is<Account>(a => a.Pat.Value == "new-pat"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePatAsync_WithNonExistingAccount_ThrowsAccountNotFoundException()
    {
        // Arrange
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns((Account?)null);

        // Act & Assert
        var act = async () => await _sut.UpdatePatAsync("no-existe", "pat");
        await act.Should().ThrowAsync<AccountNotFoundException>();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsMappedAccountInfos_WithoutPat()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new(new AccountId("acc1"), new PersonalAccessToken("tok1"), "Cuenta Uno"),
            new(new AccountId("acc2"), new PersonalAccessToken("tok2"), "Cuenta Dos", "https://custom.url")
        };
        _accounts.GetAllAsync(Arg.Any<CancellationToken>())
                 .Returns((IReadOnlyList<Account>)accounts);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert: proyección correcta, ningún PAT expuesto
        result.Should().HaveCount(2);
        result.Should().ContainSingle(a => a.AccountId == "acc1" && a.DisplayName == "Cuenta Uno");
        result.Should().ContainSingle(a => a.AccountId == "acc2" && a.TargetUrl == "https://custom.url");

        // Verificar que AccountInfo no tiene propiedad Pat en absoluto (seguridad)
        var properties = typeof(AccountInfo).GetProperties();
        properties.Should().NotContain(p => p.Name.Contains("Pat", StringComparison.OrdinalIgnoreCase));
    }
}
