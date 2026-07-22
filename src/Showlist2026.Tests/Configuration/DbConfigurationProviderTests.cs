using Showlist2026.Web.Configuration;
using Xunit;

namespace Showlist2026.Tests.Configuration;

public class DbConfigurationProviderTests
{
    // A connection string that fails fast (short timeout, unreachable host) rather than
    // hanging - CanConnect() should return false quickly instead of actually connecting.
    private const string UnreachableConnectionString =
        "Server=showlist-tests-unreachable-host,1;Database=x;Connect Timeout=1;TrustServerCertificate=True;Encrypt=False;";

    [Fact]
    public void Load_ResultsInEmptyData_WhenDatabaseUnreachable()
    {
        var provider = new DbConfigurationProvider(UnreachableConnectionString);

        provider.Load();

        Assert.False(provider.TryGet("AnyKey", out _));
    }

    [Fact]
    public void Build_ReturnsADbConfigurationProvider()
    {
        var source = new DbConfigurationSource(UnreachableConnectionString);

        var provider = source.Build(new Microsoft.Extensions.Configuration.ConfigurationBuilder());

        Assert.IsType<DbConfigurationProvider>(provider);
    }
}
