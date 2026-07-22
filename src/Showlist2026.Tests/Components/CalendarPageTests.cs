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
}
