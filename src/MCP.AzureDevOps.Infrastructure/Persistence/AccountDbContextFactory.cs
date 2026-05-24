using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MCP.AzureDevOps.Infrastructure.Persistence;

/// <summary>
/// Factory de design-time para que 'dotnet ef migrations' pueda crear el DbContext
/// sin necesidad de un startup project completamente configurado.
/// </summary>
internal sealed class AccountDbContextFactory : IDesignTimeDbContextFactory<AccountDbContext>
{
    public AccountDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccountDbContext>()
            .UseSqlite("Data Source=accounts.db")
            .Options;

        return new AccountDbContext(options);
    }
}
