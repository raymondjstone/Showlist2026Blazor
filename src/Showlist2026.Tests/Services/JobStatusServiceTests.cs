using Showlist2026.Web.Services;
using Xunit;

namespace Showlist2026.Tests.Services;

public class JobStatusServiceTests
{
    [Fact]
    public void GetRecurringJobStatuses_ReturnsEmptyList_WhenHangfireStorageNotConfigured()
    {
        // This test suite never configures Hangfire (GlobalConfiguration.UseXxxStorage), so
        // JobStorage.Current throws "has not been initialized" - GetRecurringJobStatuses
        // catches that and returns an empty list rather than propagating.
        var service = new JobStatusService();

        var result = service.GetRecurringJobStatuses();

        Assert.Empty(result);
    }
}
