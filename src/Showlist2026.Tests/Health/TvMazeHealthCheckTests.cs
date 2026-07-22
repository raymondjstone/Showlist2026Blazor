using Flurl.Http.Testing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Health;
using Xunit;

namespace Showlist2026.Tests.Health;

public class TvMazeHealthCheckTests
{
    private static TvMazeHealthCheck Make() =>
        new(Options.Create(TestFactory.Options()));

    [Fact]
    public async Task ReturnsHealthy_On200()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("{}", 200);

        var result = await Make().CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ReturnsDegraded_On429()
    {
        // Regression test for a fixed bug: Flurl throws FlurlHttpException for any non-2xx
        // response by default, so a plain `response.StatusCode == 429` check on the awaited
        // result was dead code - a real 429 never reached it and fell through to the generic
        // "Unhealthy" catch instead. Fixed by catching FlurlHttpException and checking
        // ex.StatusCode, so a rate limit now correctly reports Degraded rather than Unhealthy.
        using var httpTest = new HttpTest();
        httpTest.RespondWith("rate limited", 429);

        var result = await Make().CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task ReturnsUnhealthy_OnOtherStatusCode()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("server error", 500);

        var result = await Make().CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
        Assert.Contains("500", result.Description);
    }

    [Fact]
    public async Task ReturnsUnhealthy_WhenRequestThrows()
    {
        using var httpTest = new HttpTest();
        httpTest.SimulateException(new HttpRequestException("connection refused"));

        var result = await Make().CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }
}
