using Bunit;
using Bunit.TestDoubles;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class NextUnwatchedPageTests : BlazorTestBase
{
    public NextUnwatchedPageTests()
    {
        // NextUnwatched calls JS.InvokeVoidAsync("eval", ...) for keyboard-shortcut wiring in
        // OnAfterRenderAsync/Dispose - not relevant to the page's data logic under test, so
        // just let any JS call through instead of configuring bUnit's strict-mode JSInterop.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void RendersOneTabPerBehindBucket_ShowingEarliestUnwatchedEpisode()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10), watched: true);
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-9)); // 1 behind
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<NextUnwatched>();

        Assert.Contains("My Show", cut.Markup);
        Assert.Contains("1 Behind", cut.Markup);
    }

    [Fact]
    public void ClickingWatchedIcon_MarksEpisodeWatchedThroughRealService()
    {
        int episodeId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            var ep = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-9));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            episodeId = ep.Id;
        }

        var cut = Render<NextUnwatched>();
        cut.Find("i.fa-eye").Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
    }

    [Fact]
    public void ClickingCatchUp_MarksAllAiredEpisodesWatchedThroughRealService()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-9));
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-8));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<NextUnwatched>();
        cut.Find("button.btn-outline-success").Click();

        using var verify = Db.CreateContext();
        Assert.All(verify.Episodes.Where(e => e.show!.Id == showId), e => Assert.True(e.Watched));
    }

    [Fact]
    public void ShowsNothing_WhenNoWantedShowsHaveUnwatchedEpisodes()
    {
        var cut = Render<NextUnwatched>();

        Assert.Contains("Next Unwatched Episode Per Show (0 shows)", cut.Markup);
    }
}
