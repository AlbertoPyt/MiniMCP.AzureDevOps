namespace MCP.AzureDevOps.Domain.Tests.ValueObjects;

public class AccountIdTests
{
    [Fact]
    public void Constructor_WithValidValue_SetsValue()
    {
        var id = new AccountId("my-account");
        id.Value.Should().Be("my-account");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespace_ThrowsArgumentException(string value)
    {
        var act = () => new AccountId(value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var id = new AccountId("  account1  ");
        id.Value.Should().Be("account1");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        new AccountId("acc1").ToString().Should().Be("acc1");
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        new AccountId("acc").Should().Be(new AccountId("acc"));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        new AccountId("acc1").Should().NotBe(new AccountId("acc2"));
    }
}
