using Bunit;
using Showlist2026.Entities;
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
    public void RendersGivenUpEpisode_WithNetworkGenreAndLanguage()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show",
                network: TestData.NewNetwork("HBO"),
                webNetwork: TestData.NewWebNetwork("Hulu"),
                language: TestData.NewLanguage("English"),
                type: TestData.NewType("Scripted"));
            show.Genres = new List<Genre> { new Genre { genretext = TestData.NewGenreText("Comedy"), show = show } };
            TestData.NewEpisode(show, 1, 1, givenUp: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<GivenUp>();

        Assert.Contains("My Show", cut.Markup);
        Assert.Contains("HBO", cut.Markup);
        Assert.Contains("Hulu", cut.Markup);
        Assert.Contains("Comedy", cut.Markup);
        Assert.Contains("English", cut.Markup);
        Assert.Contains("Scripted", cut.Markup);
    }

    [Fact]
    public void RendersNetworkAndWebNetworkCountryCodes()
    {
        // Regression test: GivenUpEpisodes() included Networks/WebNetworks but never their
        // .country navigation, so GivenUpRow's "(US)"-style country annotation silently never
        // rendered even when the show's network had a country set.
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show",
                network: TestData.NewNetwork("HBO", country: TestData.NewCountry("US")),
                webNetwork: TestData.NewWebNetwork("Hulu", country: TestData.NewCountry("GB")));
            TestData.NewEpisode(show, 1, 1, givenUp: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<GivenUp>();

        Assert.Contains("US)", cut.Markup);
        Assert.Contains("GB)", cut.Markup);
    }

    [Fact]
    public void ClickingEachFilterButton_PersistsThroughRealService()
    {
        int typeId, languageId, networkId, webNetworkId, networkCountryId, genreTextId;
        using (var ctx = Db.CreateContext())
        {
            var type = TestData.NewType("Scripted");
            var language = TestData.NewLanguage("English");
            var country = TestData.NewCountry("US");
            var network = TestData.NewNetwork("HBO", country: country);
            var webNetwork = TestData.NewWebNetwork("Hulu");
            var genretext = TestData.NewGenreText("Comedy");
            var show = TestData.NewShow("My Show",
                type: type, language: language, network: network, webNetwork: webNetwork);
            show.Genres = new List<Genre> { new() { genretext = genretext, show = show } };
            TestData.NewEpisode(show, 1, 1, givenUp: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            typeId = type.Id;
            languageId = language.Id;
            networkId = network.Id;
            webNetworkId = webNetwork.Id;
            networkCountryId = country.Id;
            genreTextId = genretext.Id;
        }

        var cut = Render<GivenUp>();
        cut.Find("i[title='Select language']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Languages.Find(languageId)!.Wanted);

        cut = Render<GivenUp>();
        cut.Find("i[title='Select network']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Networks.Find(networkId)!.Wanted);

        cut = Render<GivenUp>();
        cut.Find("i[title='Select country']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Countrys.Find(networkCountryId)!.Wanted);

        cut = Render<GivenUp>();
        cut.Find("i[title='Select webnetwork']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.WebNetworks.Find(webNetworkId)!.Wanted);

        cut = Render<GivenUp>();
        cut.Find("i[title='Select type']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Types.Find(typeId)!.Wanted);

        cut = Render<GivenUp>();
        cut.Find("i[title='Select genre']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.GenreTexts.Find(genreTextId)!.Wanted);
    }

    [Fact]
    public void ClickingUndoOnMobileLayout_RemovesItFromTheList()
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
        cut.FindAll("i.fa-undo")[1].Click(); // mobile layout's undo icon

        using var verify = Db.CreateContext();
        Assert.False(verify.Episodes.Find(episodeId)!.GivenUp);
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
