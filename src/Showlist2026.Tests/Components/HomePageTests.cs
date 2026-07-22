using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class HomePageTests : BlazorTestBase
{
    [Fact]
    public void RendersShowAndEpisodeCounts()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            TestData.NewEpisode(show, 1, 1, watched: true);
            TestData.NewEpisode(show, 1, 2);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<Home>();

        Assert.Contains("1", cut.Find(".text-bg-primary .card-text").TextContent); // 1 show
        Assert.Contains("2", cut.Find(".text-bg-success .card-text").TextContent); // 2 episodes
        Assert.Contains("1", cut.Find(".text-bg-warning .card-text").TextContent); // 1 watched
    }

    [Fact]
    public void DoesNotThrow_WhenNoDataExists()
    {
        // TonightsEpisodes() is wrapped in try/catch on this page specifically because it used
        // to be a source of flaky failures; confirm the page still renders cleanly either way.
        var cut = Render<Home>();

        Assert.Contains("Shows", cut.Markup);
        Assert.DoesNotContain("spinner-border", cut.Markup);
    }
}
