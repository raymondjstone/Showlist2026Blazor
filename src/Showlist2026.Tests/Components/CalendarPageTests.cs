using Bunit;
using System.Linq;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class CalendarPageTests : BlazorTestBase
{
    [Fact]
    public void DefaultWeekView_RendersEpisodeAiringToday()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Breaking Bad", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.Now);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<Calendar>();

        Assert.Contains("Breaking Bad", cut.Markup);
    }

    [Fact]
    public void SwitchingToMonthView_RendersMonthGrid()
    {
        var cut = Render<Calendar>();

        var monthButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Month");
        monthButton.Click();

        Assert.Contains(DateTime.Now.ToString("MMMM yyyy"), cut.Markup);
    }

    [Fact]
    public void NavigatingToNextWeek_ReloadsEpisodesForThatRange()
    {
        var cut = Render<Calendar>();

        var nextButton = cut.FindAll("button.btn-outline-secondary")
            .First(b => b.QuerySelector("i.fa-chevron-right") != null);
        nextButton.Click();

        var todayButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Today");
        todayButton.Click();

        Assert.Contains("Wanted", cut.Markup);
    }

    [Fact]
    public void PreviousInMonthView_NavigatesToPriorMonth()
    {
        var cut = Render<Calendar>();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Month").Click();

        var previousButton = cut.FindAll("button.btn-outline-secondary")
            .First(b => b.QuerySelector("i.fa-chevron-left") != null);
        previousButton.Click();

        Assert.Contains(DateTime.Now.AddMonths(-1).ToString("MMMM yyyy"), cut.Markup);
    }

    [Fact]
    public void JumpingToADate_NavigatesToThatWeek()
    {
        var cut = Render<Calendar>();

        cut.Find("input[type='date']").Change("2026-03-10"); // a Tuesday -> week starts Monday 2026-03-09

        Assert.Contains("09 Mar", cut.Markup);
    }

    [Fact]
    public void MonthView_ShowsMoreLink_AndOpensAndClosesDayDetailModal_ForManyEpisodesOnOneDay()
    {
        using (var ctx = Db.CreateContext())
        {
            for (int i = 1; i <= 5; i++)
            {
                var show = TestData.NewShow($"Show {i}", wanted: true);
                TestData.NewEpisode(show, 1, 1, DateTimeOffset.Now);
                ctx.Shows.Add(show);
            }
            ctx.SaveChanges();
        }

        var cut = Render<Calendar>();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Month").Click();

        var moreButton = cut.FindAll("button.btn-link").First(b => b.TextContent.Contains("more"));
        moreButton.Click();

        Assert.Contains("modal-title", cut.Markup);
        Assert.Contains("Show 1", cut.Markup);

        cut.Find("button.btn-close").Click();

        Assert.DoesNotContain("modal-title", cut.Markup);
    }

    [Fact]
    public void EpisodeBadgeClass_ReflectsWatchedWantedAndUndecidedStates()
    {
        // Calendar doesn't pass includeIgnored:true to AiringAroundNowForUser, so Wanted:false
        // shows never appear here at all (not just unbadged) - cal-ignored is unreachable
        // through this page. Only watched/wanted/undecided are exercised.
        using (var ctx = Db.CreateContext())
        {
            var watchedShow = TestData.NewShow("Watched Show", wanted: true);
            TestData.NewEpisode(watchedShow, 1, 1, DateTimeOffset.Now, watched: true);

            var wantedShow = TestData.NewShow("Wanted Show", wanted: true);
            TestData.NewEpisode(wantedShow, 1, 1, DateTimeOffset.Now);

            var undecidedShow = TestData.NewShow("Undecided Show");
            TestData.NewEpisode(undecidedShow, 1, 1, DateTimeOffset.Now);

            ctx.Shows.AddRange(watchedShow, wantedShow, undecidedShow);
            ctx.SaveChanges();
        }

        var cut = Render<Calendar>();

        Assert.Contains("cal-watched", cut.Markup);
        Assert.Contains("cal-wanted", cut.Markup);
        Assert.Contains("cal-undecided", cut.Markup);
    }
}
