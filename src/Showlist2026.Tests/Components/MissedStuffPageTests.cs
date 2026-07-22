using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class MissedStuffPageTests : BlazorTestBase
{
    [Fact]
    public void RendersMissedEpisodes_ForWantedShows()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Missed Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-5));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<MissedStuff>();

        Assert.Contains("Missed Show", cut.Markup);
        Assert.Contains("Missed Episodes (1)", cut.Markup);
    }

    [Fact]
    public void ClickingWatchedIcon_MarksEpisodeWatchedThroughRealService_AndRemovesItFromTheList()
    {
        int episodeId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Missed Show", wanted: true);
            var ep = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-5));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            episodeId = ep.Id;
        }

        var cut = Render<MissedStuff>();
        cut.Find("i.fa-eye").Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
        Assert.Contains("Missed Episodes (0)", cut.Markup);
    }

    [Fact]
    public void ClickingCatchUp_MarksAllMissedEpisodesForThatShowWatched()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Missed Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10));
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-5));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<MissedStuff>();
        Assert.Contains("Missed Episodes (2)", cut.Markup);

        cut.Find("a[title='Catch up all missed for this show']").Click();

        Assert.Contains("Missed Episodes (0)", cut.Markup);
    }
}
