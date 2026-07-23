using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class ComingSoonPageTests : BlazorTestBase
{
    [Fact]
    public void RendersUpcomingShow_UsingIsoFormattedPremiered()
    {
        // Regression coverage: ComingSoonForUser's premiered pre-filter bug was fixed to work
        // with real ISO-formatted dates (see ComingSoonForUserTests) - confirm the page renders
        // a show using that real format end to end.
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Upcoming Show", premiered: DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"));
            show.status = "Running";
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<ComingSoon>();

        Assert.Contains("Upcoming Show", cut.Markup);
        Assert.Contains("Coming Soon (1 shows)", cut.Markup);
    }

    [Fact]
    public void StatusFilter_NarrowsListToSelectedStatus()
    {
        var premiered = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        using (var ctx = Db.CreateContext())
        {
            var running = TestData.NewShow("Running Show", premiered: premiered);
            running.status = "Running";
            var ended = TestData.NewShow("Ended Show", premiered: premiered);
            ended.status = "Ended";
            ctx.Shows.AddRange(running, ended);
            ctx.SaveChanges();
        }

        var cut = Render<ComingSoon>();
        Assert.Contains("Running Show", cut.Markup);
        Assert.Contains("Ended Show", cut.Markup);

        cut.Find("select.form-select").Change("Running");

        Assert.Contains("Running Show", cut.Markup);
        Assert.DoesNotContain("Ended Show", cut.Markup);
    }

    [Fact]
    public void RendersTypeLanguageNetworkWebNetworkAirDateAndEpisodeCounts()
    {
        var premiered = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Upcoming Show", premiered: premiered,
                type: TestData.NewType("Scripted"),
                language: TestData.NewLanguage("English"),
                network: TestData.NewNetwork("AMC"),
                webNetwork: TestData.NewWebNetwork("Netflix"));
            show.status = "Running";
            show.summary = "A great new show";
            TestData.NewEpisode(show, 1, 1, watched: true);
            TestData.NewEpisode(show, 1, 2);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<ComingSoon>();

        Assert.Contains("Scripted", cut.Markup);
        Assert.Contains("English", cut.Markup);
        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("Netflix", cut.Markup);
        Assert.Contains("A great new show", cut.Markup);
        Assert.Contains("1 / 2 watched", cut.Markup);
        Assert.Contains(DateTime.Parse(premiered).ToString("dd MMM yyyy"), cut.Markup);
    }
}
