using Bunit;
using System.Linq;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class UndecidedPageTests : BlazorTestBase
{
    [Fact]
    public void RendersEmptyTabList_WhenNothingIsUndecided()
    {
        var cut = Render<Undecided>();

        Assert.DoesNotContain("spinner-border", cut.Markup);
        Assert.Empty(cut.FindAll("li.nav-item"));
    }

    [Fact]
    public void RendersYearTab_ForUndecidedShowAiringInThePast()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Brand New Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<Undecided>();

        Assert.Contains("Brand New Show", cut.Markup);
        Assert.Single(cut.FindAll("li.nav-item"));
    }

    [Fact]
    public void SelectingAnUndecidedShow_MarksItWantedThroughRealService()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Brand New Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<Undecided>();
        cut.Find("i.far.fa-check-circle").Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.Shows.Find(showId)!.Wanted);
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
            var network = TestData.NewNetwork("AMC", country: country);
            var webNetwork = TestData.NewWebNetwork("Netflix");
            var genretext = TestData.NewGenreText("Drama");
            var show = TestData.NewShow("Undecided Show",
                type: type, language: language, network: network, webNetwork: webNetwork);
            show.Genres = new List<Showlist2026.Entities.Genre> { new() { genretext = genretext, show = show } };
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            typeId = type.Id;
            languageId = language.Id;
            networkId = network.Id;
            webNetworkId = webNetwork.Id;
            networkCountryId = country.Id;
            genreTextId = genretext.Id;
        }

        var cut = Render<Undecided>();
        cut.Find("i[title='Select type']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Types.Find(typeId)!.Wanted);

        cut = Render<Undecided>();
        cut.Find("i[title='Select language']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Languages.Find(languageId)!.Wanted);

        cut = Render<Undecided>();
        cut.Find("i[title='Select network']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Networks.Find(networkId)!.Wanted);

        cut = Render<Undecided>();
        cut.Find("i[title='Select country']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Countrys.Find(networkCountryId)!.Wanted);

        cut = Render<Undecided>();
        cut.Find("i[title='Select webnetwork']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.WebNetworks.Find(webNetworkId)!.Wanted);

        cut = Render<Undecided>();
        cut.Find("i[title='Select genre']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.GenreTexts.Find(genreTextId)!.Wanted);
    }

    [Fact]
    public void SwitchingTabs_ShowsShowsFromTheSelectedYear()
    {
        using (var ctx = Db.CreateContext())
        {
            var thisYear = TestData.NewShow("This Year Show");
            TestData.NewEpisode(thisYear, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));

            var lastYear = TestData.NewShow("Last Year Show");
            TestData.NewEpisode(lastYear, 1, 1, DateTimeOffset.UtcNow.AddYears(-1));

            ctx.Shows.AddRange(thisYear, lastYear);
            ctx.SaveChanges();
        }

        var cut = Render<Undecided>();

        var tabs = cut.FindAll("li.nav-item button");
        Assert.Equal(2, tabs.Count);

        // Earliest year tab is active by default.
        Assert.Contains("Last Year Show", cut.Markup);
        Assert.DoesNotContain("This Year Show", cut.Markup);

        tabs.First(t => !t.ClassList.Contains("active")).Click();

        Assert.Contains("This Year Show", cut.Markup);
        Assert.DoesNotContain("Last Year Show", cut.Markup);
    }

    [Fact]
    public void BulkSelectAllOnTab_MarksAllShownShowsWantedThroughRealService()
    {
        int showId1, showId2;
        using (var ctx = Db.CreateContext())
        {
            var show1 = TestData.NewShow("Show One");
            TestData.NewEpisode(show1, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            var show2 = TestData.NewShow("Show Two");
            TestData.NewEpisode(show2, 1, 1, DateTimeOffset.UtcNow.AddDays(-2));
            ctx.Shows.AddRange(show1, show2);
            ctx.SaveChanges();
            showId1 = show1.Id;
            showId2 = show2.Id;
        }

        var cut = Render<Undecided>();
        cut.Find("button.btn-success").Click(); // "Select All on Tab"

        using var verify = Db.CreateContext();
        Assert.True(verify.Shows.Find(showId1)!.Wanted);
        Assert.True(verify.Shows.Find(showId2)!.Wanted);
    }

    [Fact]
    public void BulkExcludeAllOnTab_MarksAllShownShowsExcludedThroughRealService()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Show One");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<Undecided>();
        cut.Find("button.btn-danger").Click(); // "Exclude All on Tab"

        using var verify = Db.CreateContext();
        Assert.False(verify.Shows.Find(showId)!.Wanted);
    }
}
