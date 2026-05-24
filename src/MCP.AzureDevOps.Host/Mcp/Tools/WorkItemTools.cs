namespace MCP.AzureDevOps.Host.Mcp.Tools;

[McpServerToolType]
public sealed class WorkItemTools(
    IForwardToolUseCase forwardUseCase,
    IMcpAccountContext accountContext)
{
    [McpServerTool(Name = "workitems_get")]
    [Description("Gets a work item by its ID from Azure DevOps.")]
    public async Task<string> GetWorkItemAsync(
        [Description("The work item ID")] int id,
        CancellationToken cancellationToken)
    {
        var result = await forwardUseCase.ExecuteAsync(
            new ForwardToolRequest(
                accountContext.AccountId,
                "workitems_get",
                new Dictionary<string, object?> { ["id"] = id }),
            cancellationToken);

        if (result.IsError) throw new McpException(result.Content);
        return result.Content;
    }

    [McpServerTool(Name = "workitems_create")]
    [Description("Creates a new work item in Azure DevOps.")]
    public async Task<string> CreateWorkItemAsync(
        [Description("The Azure DevOps project name")] string project,
        [Description("Work item type (e.g. 'Task', 'Bug', 'User Story')")] string type,
        [Description("Title of the work item")] string title,
        CancellationToken cancellationToken)
    {
        var result = await forwardUseCase.ExecuteAsync(
            new ForwardToolRequest(
                accountContext.AccountId,
                "workitems_create",
                new Dictionary<string, object?>
                {
                    ["project"] = project,
                    ["type"] = type,
                    ["title"] = title
                }),
            cancellationToken);

        if (result.IsError) throw new McpException(result.Content);
        return result.Content;
    }

    [McpServerTool(Name = "workitems_list")]
    [Description("Queries work items using WIQL (Work Item Query Language).")]
    public async Task<string> ListWorkItemsAsync(
        [Description("WIQL query string, e.g. SELECT [Id],[Title] FROM WorkItems WHERE [State]='Active'")] string wiql,
        CancellationToken cancellationToken)
    {
        var result = await forwardUseCase.ExecuteAsync(
            new ForwardToolRequest(
                accountContext.AccountId,
                "workitems_list",
                new Dictionary<string, object?> { ["wiql"] = wiql }),
            cancellationToken);

        if (result.IsError) throw new McpException(result.Content);
        return result.Content;
    }
}
