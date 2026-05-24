# MiniMCP.AzureDevOps

Multi-tenant proxy for the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) of Azure DevOps.

Lets multiple teams use the official Azure DevOps MCP through a single centralised server that handles per-account authentication. Supports REST API, MCP Streamable HTTP, stdio (Claude Desktop / VS Code), a CLI tool and a .NET SDK.

## Client compatibility

| Client | Transport | Status |
|--------|-----------|--------|
| Claude Desktop | stdio | ✅ |
| VS Code + GitHub Copilot | HTTP / stdio | ✅ |
| Visual Studio 2026 | HTTP / stdio | ✅ |
| GitHub Copilot (remote) | HTTP | ✅ |
| CLI (`azdevops-mcp`) | — | ✅ |
| .NET SDK | HTTP | ✅ |

## Solution structure

```
src/
├── MCP.AzureDevOps.Domain/          # Entities and Value Objects (no external deps)
├── MCP.AzureDevOps.Application/     # Use cases and ports (interfaces)
├── MCP.AzureDevOps.Infrastructure/  # HTTP gateway, account repository, encryption
├── MCP.AzureDevOps.Host/            # REST API + MCP Server (stdio and HTTP)
├── MCP.AzureDevOps.Cli/             # CLI: azdevops-mcp (dotnet global tool)
└── MCP.AzureDevOps.Sdk/             # NuGet client SDK
tests/
├── MCP.AzureDevOps.Domain.Tests/
├── MCP.AzureDevOps.Application.Tests/
└── MCP.AzureDevOps.Host.Tests/      # E2E tests (WebApplicationFactory)
```

## Quick start

### 1. Configure accounts

**Option A — appsettings (development, no database)**

Edit `src/MCP.AzureDevOps.Host/appsettings.json`:

```json
{
  "Mcp": {
    "ActiveAccountId": "my-account",
    "TargetUrl": "https://azuredevops.mcpserver.microsoft.com",
    "AccountTokens": {
      "my-account": "YOUR_PAT_HERE"
    }
  },
  "Auth": {
    "AdminApiKey": ""
  }
}
```

**Option B — SQLite database (recommended for production)**

```json
{
  "ConnectionStrings": {
    "AccountsDb": "Data Source=accounts.db"
  },
  "Mcp": {
    "TargetUrl": "https://azuredevops.mcpserver.microsoft.com",
    "DbEncryptionKey": "<32-byte base64 key — generate with: openssl rand -base64 32>"
  },
  "Auth": {
    "AdminApiKey": "<secure key — generate with: openssl rand -base64 32>"
  }
}
```

> ⚠️ Never commit `appsettings.Development.json` or any file containing real tokens. The `.gitignore` already excludes them.

### 2. Run

#### HTTP mode (REST API + MCP Streamable HTTP)

```bash
dotnet run --project src/MCP.AzureDevOps.Host
# REST API:   http://localhost:5263/api/
# MCP HTTP:   http://localhost:5263/mcp
# Swagger UI: http://localhost:5263/swagger
# Health:     http://localhost:5263/health/live
#             http://localhost:5263/health/ready
```

#### stdio mode (Claude Desktop, VS Code local, Visual Studio)

```bash
dotnet run --project src/MCP.AzureDevOps.Host -- --stdio
```

## Authentication

All REST and MCP endpoints (except `/health/*`) require the `X-Api-Key` header:

```
X-Api-Key: <value of Auth:AdminApiKey>
```

If `Auth:AdminApiKey` is empty the server starts without authentication (development only — a warning is logged on every request).

## CLI

```bash
# Install as a global dotnet tool
dotnet pack src/MCP.AzureDevOps.Cli
dotnet tool install --global --add-source ./src/MCP.AzureDevOps.Cli/nupkg MCP.AzureDevOps.Cli

# Account management
azdevops-mcp accounts list
azdevops-mcp accounts add    --account my-account --pat YOUR_PAT [--display-name "My Org"] [--url https://dev.azure.com/myorg]
azdevops-mcp accounts remove --account my-account
azdevops-mcp accounts update-pat --account my-account --pat NEW_PAT

# Tool operations
azdevops-mcp tools list --account my-account
azdevops-mcp tools call workitems_get --account my-account --args '{"id":42}'
azdevops-mcp forward workitems_get   --account my-account --args '{"id":42}'
```

## AI client configuration

### Claude Desktop

`%APPDATA%/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "azure-devops": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/src/MCP.AzureDevOps.Host", "--", "--stdio"],
      "env": {
        "Mcp__ActiveAccountId": "my-account",
        "Mcp__AccountTokens__my-account": "YOUR_PAT",
        "Mcp__TargetUrl": "https://azuredevops.mcpserver.microsoft.com"
      }
    }
  }
}
```

### VS Code + Copilot

`.vscode/mcp.json`:

```json
{
  "servers": {
    "azure-devops-mcp": {
      "type": "http",
      "url": "http://localhost:5263/mcp",
      "headers": { "X-Api-Key": "YOUR_ADMIN_API_KEY" }
    }
  }
}
```

### Visual Studio 2026

`Tools > Options > GitHub Copilot > MCP Servers` or `%USERPROFILE%/.vs/mcp.json`:

```json
{
  "servers": {
    "azure-devops-mcp": {
      "type": "http",
      "url": "http://localhost:5263/mcp",
      "headers": { "X-Api-Key": "YOUR_ADMIN_API_KEY" }
    }
  }
}
```

## .NET SDK

```csharp
await using var client = await AzureDevOpsMcpClient.CreateAsync(
    new Uri("http://localhost:5263/mcp"),
    "YOUR_PAT");

var tools  = await client.ListToolsAsync();
var result = await client.CallToolAsync("workitems_get", new Dictionary<string, object?> { ["id"] = 42 });
```

## Tests

```bash
dotnet test
```

37 tests across Domain, Application and Host (E2E with `WebApplicationFactory`).

## Requirements

- .NET 11 Preview SDK (`11.0.100-preview.4+`)
- Access to the official Azure DevOps MCP server
