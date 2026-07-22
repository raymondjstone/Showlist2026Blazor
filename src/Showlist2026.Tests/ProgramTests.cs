using Hangfire.Dashboard;
using Xunit;

namespace Showlist2026.Tests;

public class ProgramTests
{
    [Fact]
    public void AllowAllDashboardAuthorizationFilter_AlwaysAuthorizes()
    {
        // Deliberate design choice for this single-user app (documented elsewhere): the
        // Hangfire dashboard has no auth gate. This locks in that intent stays explicit.
        IDashboardAuthorizationFilter filter = new AllowAllDashboardAuthorizationFilter();

        Assert.True(filter.Authorize(null!));
    }
}
