using Showlist2026.Models;
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

    [Fact]
    public void JobStatusModel_RoundTripsAllProperties()
    {
        var last = DateTime.UtcNow.AddHours(-1);
        var next = DateTime.UtcNow.AddHours(1);
        var model = new JobStatusModel
        {
            JobName = "RefreshShows",
            Cron = "0 * * * *",
            LastExecution = last,
            NextExecution = next,
            LastStatus = "Succeeded",
            Error = "boom"
        };

        Assert.Equal("RefreshShows", model.JobName);
        Assert.Equal("0 * * * *", model.Cron);
        Assert.Equal(last, model.LastExecution);
        Assert.Equal(next, model.NextExecution);
        Assert.Equal("Succeeded", model.LastStatus);
        Assert.Equal("boom", model.Error);
    }
}
