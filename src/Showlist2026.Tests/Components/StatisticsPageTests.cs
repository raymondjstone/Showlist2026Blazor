using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class StatisticsPageTests : BlazorTestBase
{
    [Fact]
    public void RendersShowAndWatchTimeSummary()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Show", wanted: true, status: "Running");
            TestData.NewEpisode(show, 1, 1, watched: true, runtime: "60");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<Statistics>();

        Assert.Contains("Shows Tracked", cut.Markup);
        Assert.Contains("1</p>", cut.Markup); // TotalShowsTracked
        Assert.Contains("1h", cut.Markup); // 60 minutes watched
    }

    [Fact]
    public void OmitsMostWatchedShowsSection_WhenNothingWatched()
    {
        var cut = Render<Statistics>();

        Assert.DoesNotContain("Most Watched Shows", cut.Markup);
        Assert.DoesNotContain("Genre Breakdown", cut.Markup);
    }
}
