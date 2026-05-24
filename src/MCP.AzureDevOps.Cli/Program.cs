using MCP.AzureDevOps.Application.DependencyInjection;
using MCP.AzureDevOps.Application.Ports.In;
using MCP.AzureDevOps.Application.Ports.Out;
using MCP.AzureDevOps.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.Text.Json;

// ── Opciones comunes ──
var accountOption = new Option<string>("--account", "Account ID configured in appsettings.json")
{
    IsRequired = true
};

// ══════════════════════════════════════════════
// comando: forward <tool-name> --account --args
// ══════════════════════════════════════════════
var toolNameArg = new Argument<string>("tool-name", "MCP tool name to invoke");
var argsOption  = new Option<string>("--args", () => "{}", "Tool arguments as a JSON object");

var forwardCommand = new Command("forward", "Forward a tool call directly to the upstream MCP");
forwardCommand.AddArgument(toolNameArg);
forwardCommand.AddOption(accountOption);
forwardCommand.AddOption(argsOption);
forwardCommand.SetHandler(async (account, toolName, argsJson) =>
{
    using var host = BuildHost();
    var useCase   = host.Services.GetRequiredService<IForwardToolUseCase>();
    var arguments = ParseArgs(argsJson);
    var result    = await useCase.ExecuteAsync(new ForwardToolRequest(account, toolName, arguments));
    Console.WriteLine(result.Content);
    Environment.Exit(result.IsError ? 1 : 0);
}, accountOption, toolNameArg, argsOption);

// ══════════════════════════════════════════════
// comando: tools list --account
// ══════════════════════════════════════════════
var toolsListCommand = new Command("list", "List all available tools (static + dynamic from upstream)");
toolsListCommand.AddOption(accountOption);
toolsListCommand.SetHandler(async (account) =>
{
    using var host  = BuildHost();
    var useCase     = host.Services.GetRequiredService<IListToolsUseCase>();
    var tools       = await useCase.GetToolsAsync(account);
    foreach (var tool in tools)
    {
        var badge = tool.IsStatic ? "[static]" : "[dynamic]";
        Console.WriteLine($"{badge,-10} {tool.Name,-30} {tool.Description}");
    }
}, accountOption);

// ══════════════════════════════════════════════
// comando: tools call <tool-name> --account --args
// ══════════════════════════════════════════════
var callNameArg = new Argument<string>("tool-name", "Tool name to call");
var callArgsOpt = new Option<string>("--args", () => "{}", "Tool arguments as a JSON object");

var toolsCallCommand = new Command("call", "Call a specific tool");
toolsCallCommand.AddArgument(callNameArg);
toolsCallCommand.AddOption(accountOption);
toolsCallCommand.AddOption(callArgsOpt);
toolsCallCommand.SetHandler(async (account, name, argsJson) =>
{
    using var host  = BuildHost();
    var useCase     = host.Services.GetRequiredService<IForwardToolUseCase>();
    var arguments   = ParseArgs(argsJson);
    var result      = await useCase.ExecuteAsync(new ForwardToolRequest(account, name, arguments));
    Console.WriteLine(result.Content);
    Environment.Exit(result.IsError ? 1 : 0);
}, accountOption, callNameArg, callArgsOpt);

var toolsCommand = new Command("tools", "Manage and invoke MCP tools");
toolsCommand.AddCommand(toolsListCommand);
toolsCommand.AddCommand(toolsCallCommand);

// ══════════════════════════════════════════════
// comando: accounts list
// ══════════════════════════════════════════════
var accountsListCommand = new Command("list", "List all configured accounts");
accountsListCommand.SetHandler(async () =>
{
    using var host  = BuildHost();
    var useCase     = host.Services.GetRequiredService<IManageAccountsUseCase>();
    var accounts    = await useCase.GetAllAsync();
    if (!accounts.Any())
    {
        Console.WriteLine("No accounts configured. Add entries to appsettings.json under Mcp.AccountTokens.");
        return;
    }
    foreach (var acc in accounts)
        Console.WriteLine($"{acc.AccountId,-30} {acc.DisplayName}");
});

// ══════════════════════════════════════════════
// comando: accounts add --account --pat [--display-name] [--url]
// ══════════════════════════════════════════════
var addPatOption         = new Option<string>("--pat", "Personal Access Token for the account") { IsRequired = true };
var addDisplayNameOption = new Option<string?>("--display-name", "Optional display name");
var addUrlOption         = new Option<string?>("--url", "Optional Azure DevOps organization URL");

var accountsAddCommand = new Command("add", "Register a new account");
accountsAddCommand.AddOption(accountOption);
accountsAddCommand.AddOption(addPatOption);
accountsAddCommand.AddOption(addDisplayNameOption);
accountsAddCommand.AddOption(addUrlOption);
accountsAddCommand.SetHandler(async (account, pat, displayName, url) =>
{
    using var host = BuildHost();
    var useCase    = host.Services.GetRequiredService<IManageAccountsUseCase>();
    await useCase.RegisterAsync(new RegisterAccountRequest(
        AccountId:   account,
        Pat:         pat,
        DisplayName: displayName,
        TargetUrl:   url));
    Console.WriteLine($"Account '{account}' registered successfully.");
}, accountOption, addPatOption, addDisplayNameOption, addUrlOption);

// ══════════════════════════════════════════════
// comando: accounts remove --account
// ══════════════════════════════════════════════
var accountsRemoveCommand = new Command("remove", "Remove a registered account");
accountsRemoveCommand.AddOption(accountOption);
accountsRemoveCommand.SetHandler(async (account) =>
{
    using var host = BuildHost();
    var useCase    = host.Services.GetRequiredService<IManageAccountsUseCase>();
    await useCase.RemoveAsync(account);
    Console.WriteLine($"Account '{account}' removed.");
}, accountOption);

// ══════════════════════════════════════════════
// comando: accounts update-pat --account --pat
// ══════════════════════════════════════════════
var updatePatOption = new Option<string>("--pat", "New Personal Access Token") { IsRequired = true };

var accountsUpdatePatCommand = new Command("update-pat", "Update the PAT for an existing account");
accountsUpdatePatCommand.AddOption(accountOption);
accountsUpdatePatCommand.AddOption(updatePatOption);
accountsUpdatePatCommand.SetHandler(async (account, pat) =>
{
    using var host = BuildHost();
    var useCase    = host.Services.GetRequiredService<IManageAccountsUseCase>();
    await useCase.UpdatePatAsync(account, pat);
    Console.WriteLine($"PAT updated for account '{account}'.");
}, accountOption, updatePatOption);

var accountsCommand = new Command("accounts", "Manage configured accounts");
accountsCommand.AddCommand(accountsListCommand);
accountsCommand.AddCommand(accountsAddCommand);
accountsCommand.AddCommand(accountsRemoveCommand);
accountsCommand.AddCommand(accountsUpdatePatCommand);

// ══════════════════════════════════════════════
// Root command
// ══════════════════════════════════════════════
var rootCommand = new RootCommand("Azure DevOps MCP CLI — interact with the MCP proxy server");
rootCommand.AddCommand(forwardCommand);
rootCommand.AddCommand(toolsCommand);
rootCommand.AddCommand(accountsCommand);

return await rootCommand.InvokeAsync(args);

// ── Helpers ──
static IHost BuildHost() =>
    Host.CreateDefaultBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddInfrastructureServices(ctx.Configuration);
            services.AddApplicationServices();
            // Tools estáticas vacías para el CLI (no necesita el listado completo)
            services.AddSingleton<IEnumerable<ToolDescriptor>>(Array.Empty<ToolDescriptor>());
        })
        .Build();

static IReadOnlyDictionary<string, object?> ParseArgs(string json)
{
    if (string.IsNullOrWhiteSpace(json) || json == "{}")
        return new Dictionary<string, object?>();

    return JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
        ?? new Dictionary<string, object?>();
}
