using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class AiringAroundNowPageTests : BlazorTestBase
{
    [Fact]
    public void RendersMissedTab_ForWantedShowWithAnAiredEpisode()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Wanted Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<AiringAroundNow>();

        Assert.Contains("Missed", cut.Markup);
        Assert.Contains("Wanted Show", cut.Markup);
    }

    [Fact]
    public void ClickingWatchedIcon_MarksEpisodeWatchedThroughRealService()
    {
        int episodeId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Wanted Show", wanted: true);
            var ep = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            episodeId = ep.Id;
        }

        var cut = Render<AiringAroundNow>();
        cut.Find("i.fa-eye").Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
    }
}
