using Bunit;
using Showlist2026.Entities;
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
            var show = TestData.NewShow("Wanted Show", wanted: true,
                network: TestData.NewNetwork("AMC"),
                webNetwork: TestData.NewWebNetwork("Netflix"),
                language: TestData.NewLanguage("English"),
                type: TestData.NewType("Scripted"));
            show.Genres = new List<Genre> { new Genre { genretext = TestData.NewGenreText("Drama"), show = show } };
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<AiringAroundNow>();

        Assert.Contains("Missed", cut.Markup);
        Assert.Contains("Wanted Show", cut.Markup);
        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("Netflix", cut.Markup);
        Assert.Contains("Drama", cut.Markup);
        Assert.Contains("English", cut.Markup);
        Assert.Contains("Scripted", cut.Markup);
        Assert.Contains("fa-folder-plus", cut.Markup); // no FolderName set yet
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

    [Fact]
    public void RendersNewTab_ForUndecidedShowsFirstEpisode()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Brand New Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(5));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<AiringAroundNow>();

        Assert.Contains("New", cut.Markup);
        Assert.Contains("Brand New Show", cut.Markup);
    }

    [Fact]
    public void SelectingAnUndecidedShow_MarksItWantedThroughRealService()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Brand New Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(5));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<AiringAroundNow>();
        cut.Find("i.fa-check-circle").Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.Shows.Find(showId)!.Wanted);
    }
}
