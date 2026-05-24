namespace MCP.AzureDevOps.Host.Mcp.Tools;

[McpServerToolType]
public sealed class RepositoriesTools(
    IForwardToolUseCase forwardUseCase,
    IMcpAccountContext accountContext)
{
    [McpServerTool(Name = "repos_list")]
    [Description("Lists all repositories in an Azure DevOps project.")]
    public async Task<string> ListRepositoriesAsync(
        [Description("The Azure DevOps project name")] string project,
        CancellationToken cancellationToken)
    {
        var result = await forwardUseCase.ExecuteAsync(
            new ForwardToolRequest(
                accountContext.AccountId,
                "repos_list",
                new Dictionary<string, object?> { ["project"] = project }),
            cancellationToken);

        if (result.IsError) throw new McpException(result.Content);
        return result.Content;
    }

    [McpServerTool(Name = "repos_get_prs")]
    [Description("Lists pull requests in an Azure DevOps repository.")]
    public async Task<string> GetPullRequestsAsync(
        [Description("The Azure DevOps project name")] string project,
        [Description("The repository name")] string repositoryName,
        CancellationToken cancellationToken)
    {
        var result = await forwardUseCase.ExecuteAsync(
            new ForwardToolRequest(
                accountContext.AccountId,
                "repos_get_prs",
                new Dictionary<string, object?>
                {
                    ["project"] = project,
                    ["repositoryName"] = repositoryName
                }),
            cancellationToken);

        if (result.IsError) throw new McpException(result.Content);
        return result.Content;
    }
}
