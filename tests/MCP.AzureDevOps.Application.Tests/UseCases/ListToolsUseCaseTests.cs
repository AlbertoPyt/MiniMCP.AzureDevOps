namespace MCP.AzureDevOps.Application.Tests.UseCases;

public class ListToolsUseCaseTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUpstreamMcpGateway _gateway  = Substitute.For<IUpstreamMcpGateway>();

    [Fact]
    public async Task GetToolsAsync_CombinesStaticAndDynamic_WithoutDuplicates()
    {
        // Arrange
        var account = new Account(new AccountId("acc1"), new PersonalAccessToken("tok"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(account);

        var staticTools = new[]
        {
            new ToolDescriptor("workitems_get", "Get WI",       "{}", IsStatic: true),
            new ToolDescriptor("pipelines_run", "Run pipeline", "{}", IsStatic: true)
        };

        var upstreamTools = new[]
        {
            new ToolDescriptor("workitems_get", "WI from upstream (duplicate)", "{}", IsStatic: false),
            new ToolDescriptor("projects_list", "List projects",                "{}", IsStatic: false)
        };

        _gateway.ListToolsAsync(Arg.Any<PersonalAccessToken>(), Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<ToolDescriptor>)upstreamTools.ToList());

        var sut = new ListToolsUseCase(_accounts, _gateway, staticTools);

        // Act
        var result = await sut.GetToolsAsync("acc1");

        // Assert: 2 static + 1 new dynamic (no duplicates)
        result.Should().HaveCount(3);
        result.Should().ContainSingle(t => t.Name == "workitems_get" && t.IsStatic);
        result.Should().ContainSingle(t => t.Name == "projects_list" && !t.IsStatic);
        result.Should().NotContain(t => t.Name == "workitems_get" && !t.IsStatic);
    }

    [Fact]
    public async Task GetToolsAsync_WhenUpstreamUnavailable_ReturnsOnlyStaticTools()
    {
        // Arrange
        var account = new Account(new AccountId("acc1"), new PersonalAccessToken("tok"));
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns(account);

        var staticTools = new[]
        {
            new ToolDescriptor("workitems_get", "Get WI", "{}", IsStatic: true)
        };

        _gateway.ListToolsAsync(Arg.Any<PersonalAccessToken>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<IReadOnlyList<ToolDescriptor>>(
                    new HttpRequestException("Upstream unavailable")));

        var sut = new ListToolsUseCase(_accounts, _gateway, staticTools);

        // Act
        var result = await sut.GetToolsAsync("acc1");

        // Assert: degradación elegante - solo tools estáticas
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("workitems_get");
    }

    [Fact]
    public async Task GetToolsAsync_WhenAccountNotFound_ThrowsAccountNotFoundException()
    {
        // Arrange
        _accounts.FindByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
                 .Returns((Account?)null);
        var sut = new ListToolsUseCase(_accounts, _gateway, Array.Empty<ToolDescriptor>());

        // Act & Assert
        var act = async () => await sut.GetToolsAsync("unknown");
        await act.Should().ThrowAsync<AccountNotFoundException>();
    }
}
