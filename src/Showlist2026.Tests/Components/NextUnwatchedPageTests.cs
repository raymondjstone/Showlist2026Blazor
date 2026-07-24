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
    public async Task HandleKeyPressStatic_IsANoOpRequiredForJsInterop()
    {
        // The instance-bound key handler does the real work; this static stub only exists
        // because [JSInvokable] methods must be static or instance-callable via a DotNetObjectReference
        // - covered directly since nothing in the browser ever calls back into it during tests.
        await Showlist2026.Web.Components.Pages.NextUnwatched.HandleKeyPressStatic("j");
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
    public void ClickingMobileWatchedIcon_MarksEpisodeWatchedThroughRealService()
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
        var eyeIcons = cut.FindAll("i.fa-eye");
        Assert.True(eyeIcons.Count >= 2); // desktop + mobile
        eyeIcons[1].Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
    }

    [Fact]
    public void ClickingMobileCatchUp_MarksAllAiredEpisodesWatchedThroughRealService()
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
        var catchUpButtons = cut.FindAll("button.btn-outline-success");
        Assert.True(catchUpButtons.Count >= 2); // desktop + mobile
        catchUpButtons[1].Click();

        using var verify = Db.CreateContext();
        Assert.All(verify.Episodes.Where(e => e.show!.Id == showId), e => Assert.True(e.Watched));
    }

    [Theory]
    [InlineData("Ended", "bg-secondary")]
    [InlineData("To Be Determined", "bg-warning text-dark")]
    [InlineData("In Development", "bg-info")]
    [InlineData("Something Else", "bg-light text-dark")]
    public void RendersStatusBadge_ForEveryStatusVariant(string status, string expectedClass)
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true, status: status);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-9));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<NextUnwatched>();

        Assert.Contains($"{expectedClass}\">{status}", cut.Markup);
    }

    [Theory]
    [InlineData(2, "bg-warning text-dark\">Med")]
    [InlineData(1, "bg-info\">Low")]
    public void RendersPriorityBadge_ForMediumAndLowPriority(int priority, string expectedBadge)
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            show.Priority = priority;
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-9));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<NextUnwatched>();

        Assert.Contains(expectedBadge, cut.Markup);
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

    [Fact]
    public void RendersProgressBarLanguageNetworkStatusAndPriorityBadges()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true,
                network: TestData.NewNetwork("AMC"),
                webNetwork: TestData.NewWebNetwork("Netflix"),
                language: TestData.NewLanguage("English"),
                status: "Running");
            show.Priority = 3; // High
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10), watched: true);
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-9)); // next unwatched
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<NextUnwatched>();

        Assert.Contains("English", cut.Markup);
        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("Netflix", cut.Markup);
        Assert.Contains("bg-success\">Running", cut.Markup);
        Assert.Contains("bg-danger\">High", cut.Markup);
        Assert.Contains("progress-bar", cut.Markup);
        Assert.Contains("1/2", cut.Markup); // 1 watched of 2 aired
    }

    [Theory]
    [InlineData("name")]
    [InlineData("date")]
    [InlineData("priority")]
    public void ChangingSortOrder_ReloadsWithoutError(string sortValue)
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-9));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<NextUnwatched>();
        cut.Find("select.form-select-sm").Change(sortValue);

        Assert.Contains("My Show", cut.Markup);
    }

    [Fact]
    public void SwitchingTabs_ShowsTheSelectedBehindBucket()
    {
        using (var ctx = Db.CreateContext())
        {
            var oneBehind = TestData.NewShow("One Behind Show", wanted: true);
            TestData.NewEpisode(oneBehind, 1, 1, DateTimeOffset.UtcNow.AddDays(-9));

            var manyBehind = TestData.NewShow("Many Behind Show", wanted: true);
            for (int i = 1; i <= 3; i++)
                TestData.NewEpisode(manyBehind, 1, i, DateTimeOffset.UtcNow.AddDays(-9 + i));

            ctx.Shows.AddRange(oneBehind, manyBehind);
            ctx.SaveChanges();
        }

        var cut = Render<NextUnwatched>();

        // Tabs render in fixed bucket order (1 / 2-5 / 6-20 / 20+), not by count - "1 Behind" is
        // first and therefore active by default.
        Assert.Contains("One Behind Show", cut.Markup);
        Assert.DoesNotContain("Many Behind Show", cut.Markup);

        cut.FindAll("li.nav-item button").First(b => b.TextContent.Contains("2-5 Behind")).Click();

        Assert.Contains("Many Behind Show", cut.Markup);
        Assert.DoesNotContain("One Behind Show", cut.Markup);
    }
}
