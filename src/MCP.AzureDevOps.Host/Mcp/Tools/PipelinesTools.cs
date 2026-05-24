namespace MCP.AzureDevOps.Host.Mcp.Tools;

[McpServerToolType]
public sealed class PipelinesTools(
    IForwardToolUseCase forwardUseCase,
    IMcpAccountContext accountContext)
{
    [McpServerTool(Name = "pipelines_list")]
    [Description("Lists pipelines in an Azure DevOps project.")]
    public async Task<string> ListPipelinesAsync(
        [Description("The Azure DevOps project name")] string project,
        CancellationToken cancellationToken)
    {
        var result = await forwardUseCase.ExecuteAsync(
            new ForwardToolRequest(
                accountContext.AccountId,
                "pipelines_list",
                new Dictionary<string, object?> { ["project"] = project }),
            cancellationToken);

        if (result.IsError) throw new McpException(result.Content);
        return result.Content;
    }

    [McpServerTool(Name = "pipelines_run")]
    [Description("Triggers a pipeline run in Azure DevOps.")]
    public async Task<string> RunPipelineAsync(
        [Description("The Azure DevOps project name")] string project,
        [Description("The pipeline ID")] int pipelineId,
        CancellationToken cancellationToken)
    {
        var result = await forwardUseCase.ExecuteAsync(
            new ForwardToolRequest(
                accountContext.AccountId,
                "pipelines_run",
                new Dictionary<string, object?>
                {
                    ["project"] = project,
                    ["pipelineId"] = pipelineId
                }),
            cancellationToken);

        if (result.IsError) throw new McpException(result.Content);
        return result.Content;
    }
}
