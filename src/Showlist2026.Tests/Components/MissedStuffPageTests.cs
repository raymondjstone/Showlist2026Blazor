using Bunit;
using System.Linq;
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

    [Fact]
    public void RendersNetworkWebNetworkCountriesAndMultipleGenres()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Missed Show", wanted: true,
                network: TestData.NewNetwork("AMC", country: TestData.NewCountry("US")),
                webNetwork: TestData.NewWebNetwork("Netflix", country: TestData.NewCountry("GB")));
            show.Genres = new List<Showlist2026.Entities.Genre>
            {
                new() { genretext = TestData.NewGenreText("Drama"), show = show },
                new() { genretext = TestData.NewGenreText("Crime"), show = show }
            };
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-5));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<MissedStuff>();

        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("US)", cut.Markup);
        Assert.Contains("Netflix", cut.Markup);
        Assert.Contains("GB)", cut.Markup);
        Assert.Contains("Drama", cut.Markup);
        Assert.Contains("Crime", cut.Markup);
    }

    [Fact]
    public void RendersSearchLinks_ForActiveTvSites()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.TVSites.Add(new Showlist2026.Entities.TVSite
            {
                Name = "MySite", URLTemplate = "http://example.com/{URLSearchTerm}", Active = true, Order = 1
            });
            var show = TestData.NewShow("Missed Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-5));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<MissedStuff>();

        Assert.Contains("fa-magnifying-glass", cut.Markup);
    }

    [Fact]
    public void ClickingGivenUpIcon_Desktop_MarksEpisodeGivenUpThroughRealService()
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
        cut.FindAll("i.fa-flag")[0].Click(); // desktop

        using var verify = Db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.GivenUp);
        Assert.Contains("Missed Episodes (0)", cut.Markup);
    }

    [Fact]
    public void ClickingGivenUpIcon_Mobile_MarksEpisodeGivenUpThroughRealService()
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
        cut.FindAll("i.fa-flag")[1].Click(); // mobile

        using var verify = Db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.GivenUp);
        Assert.Contains("Missed Episodes (0)", cut.Markup);
    }

    [Fact]
    public void ClickingWatchedIcon_Mobile_MarksEpisodeWatchedThroughRealService()
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
        cut.FindAll("i.fa-eye")[1].Click(); // mobile

        using var verify = Db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
        Assert.Contains("Missed Episodes (0)", cut.Markup);
    }

    [Fact]
    public void ClickingCatchUp_Mobile_MarksAllMissedEpisodesForThatShowWatched()
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

        cut.FindAll("a[title='Catch up all missed for this show']")[1].Click(); // mobile

        Assert.Contains("Missed Episodes (0)", cut.Markup);
    }
}
