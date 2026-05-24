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
    public async Task RegisterAsync_WithNewAccount_AddsToRepository_AndReturnsInfo()
    {
        // Arrange
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns((Account?)null);

        var request = new RegisterAccountRequest("equipo-a", "mi-pat", "Equipo A");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert: repositorio recibe el AddAsync
        await _accounts.Received(1).AddAsync(
            Arg.Is<Account>(a => a.Id.Value == "equipo-a" && a.DisplayName == "Equipo A"),
            Arg.Any<CancellationToken>());

        // Assert: se devuelve la info sin hacer una segunda consulta a la BBDD
        result.AccountId.Should().Be("equipo-a");
        result.DisplayName.Should().Be("Equipo A");
        await _accounts.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await _accounts.DidNotReceive().GetAllInfoAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateAccount_ThrowsInvalidOperationException()
    {
        var existing = new Account(new AccountId("equipo-a"), new PersonalAccessToken("old-pat"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(existing);

        var act = async () => await _sut.RegisterAsync(
            new RegisterAccountRequest("equipo-a", "new-pat"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*equipo-a*");

        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_CallsDeleteDirectly_WithoutPrefetch()
    {
        // Arrange: DeleteAsync completará sin excepción (cuenta existe)
        _accounts.DeleteAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(Task.CompletedTask);

        // Act
        await _sut.RemoveAsync("equipo-a");

        // Assert: solo DeleteAsync, nunca FindByIdAsync
        await _accounts.Received(1).DeleteAsync(
            Arg.Is<AccountId>(id => id.Value == "equipo-a"),
            Arg.Any<CancellationToken>());
        await _accounts.DidNotReceive()
            .FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_WhenDeleteThrowsNotFound_PropagatesException()
    {
        // Arrange: el repositorio lanza si no encuentra la cuenta
        _accounts.DeleteAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(Task.FromException(new AccountNotFoundException("no-existe")));

        var act = async () => await _sut.RemoveAsync("no-existe");
        await act.Should().ThrowAsync<AccountNotFoundException>();
    }

    // ── UpdatePatAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePatAsync_WithExistingAccount_UpdatesRepository()
    {
        var account = new Account(new AccountId("equipo-a"), new PersonalAccessToken("old-pat"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(account);

        await _sut.UpdatePatAsync("equipo-a", "new-pat");

        await _accounts.Received(1).UpdateAsync(
            Arg.Is<Account>(a => a.Pat.Value == "new-pat"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePatAsync_WithNonExistingAccount_ThrowsAccountNotFoundException()
    {
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns((Account?)null);

        var act = async () => await _sut.UpdatePatAsync("no-existe", "pat");
        await act.Should().ThrowAsync<AccountNotFoundException>();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_DelegatesToGetAllInfoAsync_WithoutDecryptingPats()
    {
        // Arrange: el repositorio ya devuelve AccountInfo (sin PATs)
        var infos = new List<AccountInfo>
        {
            new("acc1", "Cuenta Uno", null,                    DateTimeOffset.UtcNow),
            new("acc2", "Cuenta Dos", "https://custom.url",    DateTimeOffset.UtcNow)
        };
        _accounts.GetAllInfoAsync(Arg.Any<CancellationToken>())
                 .Returns((IReadOnlyList<AccountInfo>)infos);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert: delega directamente; nunca llama a GetAllAsync (que descifra PATs)
        result.Should().HaveCount(2);
        result.Should().ContainSingle(a => a.AccountId == "acc1" && a.DisplayName == "Cuenta Uno");
        result.Should().ContainSingle(a => a.AccountId == "acc2" && a.TargetUrl == "https://custom.url");

        await _accounts.Received(1).GetAllInfoAsync(Arg.Any<CancellationToken>());
        await _accounts.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());

        // Verificar que AccountInfo no expone el PAT
        typeof(AccountInfo).GetProperties()
            .Should().NotContain(p => p.Name.Contains("Pat", StringComparison.OrdinalIgnoreCase));
    }
}
