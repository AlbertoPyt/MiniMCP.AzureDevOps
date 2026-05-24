using FluentAssertions;
using MCP.AzureDevOps.Domain.ValueObjects;

namespace MCP.AzureDevOps.Domain.Tests.ValueObjects;

public class PersonalAccessTokenTests
{
    [Fact]
    public void Constructor_WithValidPat_SetsValue()
    {
        var pat = new PersonalAccessToken("my-secret-token");
        pat.Value.Should().Be("my-secret-token");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespace_ThrowsArgumentException(string value)
    {
        var act = () => new PersonalAccessToken(value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var pat = new PersonalAccessToken("  token123  ");
        pat.Value.Should().Be("token123");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        new PersonalAccessToken("tok").Should().Be(new PersonalAccessToken("tok"));
    }
}
