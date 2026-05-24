namespace MCP.AzureDevOps.Application.Tests.UseCases;

public class ForwardToolUseCaseTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUpstreamMcpGateway _gateway  = Substitute.For<IUpstreamMcpGateway>();
    private readonly ForwardToolUseCase _sut;

    public ForwardToolUseCaseTests()
    {
        _sut = new ForwardToolUseCase(_accounts, _gateway);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccountExists_CallsGatewayAndReturnsResult()
    {
        // Arrange
        var account = new Account(new AccountId("acc1"), new PersonalAccessToken("tok123"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(account);
        _gateway.CallToolAsync(
            Arg.Any<PersonalAccessToken>(),
            "workitems_get",
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ToolExecutionResult(false, "Work item content"));

        // Act
        var result = await _sut.ExecuteAsync(
            new ForwardToolRequest("acc1", "workitems_get",
                new Dictionary<string, object?> { ["id"] = 42 }));

        // Assert
        result.IsError.Should().BeFalse();
        result.Content.Should().Be("Work item content");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccountNotFound_ThrowsAccountNotFoundException()
    {
        // Arrange
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns((Account?)null);

        // Act & Assert
        var act = async () => await _sut.ExecuteAsync(
            new ForwardToolRequest("unknown-account", "some_tool",
                new Dictionary<string, object?>()));

        await act.Should().ThrowAsync<AccountNotFoundException>()
            .WithMessage("*unknown-account*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenGatewayReturnsError_ReturnsErrorResult()
    {
        // Arrange
        var account = new Account(new AccountId("acc1"), new PersonalAccessToken("tok123"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(account);
        _gateway.CallToolAsync(
            Arg.Any<PersonalAccessToken>(),
            Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<CancellationToken>())
            .Returns(new ToolExecutionResult(true, "Upstream error message"));

        // Act
        var result = await _sut.ExecuteAsync(
            new ForwardToolRequest("acc1", "some_tool", new Dictionary<string, object?>()));

        // Assert
        result.IsError.Should().BeTrue();
        result.Content.Should().Be("Upstream error message");
    }
}
