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
}
