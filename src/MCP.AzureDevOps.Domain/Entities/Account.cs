using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Domain.Entities;

public sealed class Account
{
    public AccountId Id { get; }
    public PersonalAccessToken Pat { get; }

    public Account(AccountId id, PersonalAccessToken pat)
    {
        Id = id;
        Pat = pat;
    }
}
