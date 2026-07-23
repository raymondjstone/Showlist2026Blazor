using Bunit;
using System.Linq;
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
    public void RendersNetworkAndWebNetworkCountryCodes()
    {
        // Regression test: AiringAroundNowForUser's three queries included Networks/WebNetworks
        // but never their .country navigation, so EpisodeRow's "(US)"-style country annotation
        // silently never rendered even when the show's network had a country set.
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Wanted Show", wanted: true,
                network: TestData.NewNetwork("AMC", country: TestData.NewCountry("US")),
                webNetwork: TestData.NewWebNetwork("Netflix", country: TestData.NewCountry("GB")));
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<AiringAroundNow>();

        Assert.Contains("US)", cut.Markup);
        Assert.Contains("GB)", cut.Markup);
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
    public void RendersSeparateDayTabs_ForFutureSelectedEpisodesOnDifferentDays_WithOrdinalSuffix()
    {
        var firstDate = DateTimeOffset.UtcNow.AddDays(3);
        var secondDate = DateTimeOffset.UtcNow.AddDays(4);
        using (var ctx = Db.CreateContext())
        {
            var show1 = TestData.NewShow("Show One", wanted: true);
            TestData.NewEpisode(show1, 1, 1, firstDate);
            var show2 = TestData.NewShow("Show Two", wanted: true);
            TestData.NewEpisode(show2, 1, 1, secondDate);
            ctx.Shows.AddRange(show1, show2);
            ctx.SaveChanges();
        }

        var cut = Render<AiringAroundNow>();

        string Suffix(int day) => (day == 11 || day == 12 || day == 13) ? "th" : (day % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
        var firstTabName = firstDate.ToLocalTime().Date.ToString("dd") + Suffix(firstDate.ToLocalTime().Date.Day);
        var secondTabName = secondDate.ToLocalTime().Date.ToString("dd") + Suffix(secondDate.ToLocalTime().Date.Day);

        Assert.Contains(firstTabName, cut.Markup);
        Assert.Contains(secondTabName, cut.Markup);

        // Switch to the other date tab and confirm its content swaps in.
        var otherTabButton = cut.FindAll("button.nav-link").First(b => !b.TextContent.Contains(firstTabName));
        otherTabButton.Click();
        Assert.Contains("Show Two", cut.Markup);
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
