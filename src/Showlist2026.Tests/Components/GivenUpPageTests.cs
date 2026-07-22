using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class GivenUpPageTests : BlazorTestBase
{
    [Fact]
    public void ShowsEmptyMessage_WhenNoGivenUpEpisodes()
    {
        var cut = Render<GivenUp>();

        Assert.Contains("No episodes marked as given up", cut.Markup);
    }

    [Fact]
    public void RendersGivenUpEpisode_AndUndoRemovesItFromTheList()
    {
        int episodeId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show");
            var ep = TestData.NewEpisode(show, 1, 1, givenUp: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            episodeId = ep.Id;
        }

        var cut = Render<GivenUp>();
        Assert.Contains("My Show", cut.Markup);

        cut.Find("i.fa-undo").Click();

        using var verify = Db.CreateContext();
        Assert.False(verify.Episodes.Find(episodeId)!.GivenUp);
        Assert.Contains("No episodes marked as given up", cut.Markup);
    }
}
