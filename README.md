# MiniMCP.AzureDevOps

Proxy multi-tenant para el [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) de Azure DevOps.

Permite que múltiples equipos usen el MCP oficial de Azure DevOps a través de un único servidor centralizado que gestiona la autenticación por cuenta.

## Compatibilidad

| Cliente | Transporte | Estado |
|---------|-----------|--------|
| Claude Desktop | stdio | ✅ |
| VS Code + GitHub Copilot | HTTP / stdio | ✅ |
| Visual Studio 2026 | HTTP / stdio | ✅ |
| GitHub Copilot (remoto) | HTTP | ✅ |
| CLI (`azdevops-mcp`) | — | ✅ |
| SDK .NET | HTTP | ✅ |

## Estructura

```
src/
├── MCP.AzureDevOps.Domain/          # Entidades y Value Objects
├── MCP.AzureDevOps.Application/     # Casos de uso y puertos (interfaces)
├── MCP.AzureDevOps.Infrastructure/  # Gateway HTTP y repositorio de cuentas
├── MCP.AzureDevOps.Host/            # REST API + MCP Server (stdio y HTTP)
├── MCP.AzureDevOps.Cli/             # CLI: azdevops-mcp (dotnet tool)
└── MCP.AzureDevOps.Sdk/             # NuGet client SDK
tests/
├── MCP.AzureDevOps.Domain.Tests/
└── MCP.AzureDevOps.Application.Tests/
```

## Configuración rápida

Edita `src/MCP.AzureDevOps.Host/appsettings.json`:

```json
{
  "Mcp": {
    "ActiveAccountId": "mi-cuenta",
    "TargetUrl": "https://azuredevops.mcpserver.microsoft.com",
    "AccountTokens": {
      "mi-cuenta": "TU_PAT_AQUI"
    }
  }
}
```

> ⚠️ Nunca subas `appsettings.Development.json` ni archivos con tokens reales. El `.gitignore` ya los excluye.

## Uso

### Modo HTTP (REST API + MCP Streamable HTTP)
```bash
dotnet run --project src/MCP.AzureDevOps.Host
# REST API:   http://localhost:5263/api/
# MCP HTTP:   http://localhost:5263/mcp
# Swagger UI: http://localhost:5263/swagger
```

### Modo stdio (Claude Desktop, VS Code local, Visual Studio)
```bash
dotnet run --project src/MCP.AzureDevOps.Host -- --stdio
```

### CLI
```bash
# Instalar como dotnet tool global
dotnet pack src/MCP.AzureDevOps.Cli
dotnet tool install --global --add-source ./src/MCP.AzureDevOps.Cli/nupkg MCP.AzureDevOps.Cli

# Usar
azdevops-mcp tools list --account mi-cuenta
azdevops-mcp tools call workitems_get --account mi-cuenta --args '{"id":42}'
azdevops-mcp accounts list
```

## Configuración de clientes AI

### Claude Desktop
`%APPDATA%/Claude/claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "azure-devops": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/ruta/src/MCP.AzureDevOps.Host", "--", "--stdio"],
      "env": {
        "Mcp__ActiveAccountId": "mi-cuenta",
        "Mcp__AccountTokens__mi-cuenta": "TU_PAT",
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
      "url": "http://localhost:5263/mcp"
    }
  }
}
```

### Visual Studio 2026
`Tools > Options > GitHub Copilot > MCP Servers` o `%USERPROFILE%/.vs/mcp.json`:
```json
{
  "servers": {
    "azure-devops-mcp": {
      "type": "http",
      "url": "http://localhost:5263/mcp"
    }
  }
}
```

## SDK .NET

```csharp
await using var client = await AzureDevOpsMcpClient.CreateAsync(
    new Uri("http://localhost:5263/mcp"),
    "TU_PAT");

var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync("workitems_get", new Dictionary<string, object?> { ["id"] = 42 });
```

## Tests

```bash
dotnet test
```

## Requisitos

- .NET 11 Preview SDK (`11.0.100-preview.4+`)
- Acceso al MCP oficial de Azure DevOps
