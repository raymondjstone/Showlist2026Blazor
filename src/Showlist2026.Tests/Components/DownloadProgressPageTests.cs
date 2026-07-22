using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class DownloadProgressPageTests : BlazorTestBase
{
    [Fact]
    public void RendersPercentCompleteAndMissingCount()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-5), watched: true);
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-4)); // missing
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<DownloadProgress>();

        Assert.Contains("My Show", cut.Markup);
        Assert.Contains("50%", cut.Markup);
        Assert.Contains("1 shows with missing episodes", cut.Markup);
    }

    [Fact]
    public void TogglingShowMissing_ExpandsAndCollapsesTheMissingEpisodeList()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            TestData.NewEpisode(show, 1, 5, DateTimeOffset.UtcNow.AddDays(-4));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<DownloadProgress>();
        Assert.DoesNotContain("S01E05", cut.Markup);

        cut.Find("button.btn-link").Click();
        Assert.Contains("S01E05", cut.Markup);

        cut.Find("button.btn-link").Click();
        Assert.DoesNotContain("S01E05", cut.Markup);
    }
}
