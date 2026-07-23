using Bunit;
using System.Linq;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class ShowDetailPageTests : BlazorTestBase
{
    private int SeedShowWithEpisodes(bool wanted = true)
    {
        using var ctx = Db.CreateContext();
        var show = TestData.NewShow("Breaking Bad", wanted: wanted, network: TestData.NewNetwork("AMC"));
        TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30), watched: true);
        TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-23));
        TestData.NewEpisode(show, 1, 3, DateTimeOffset.UtcNow.AddDays(7));
        ctx.Shows.Add(show);
        ctx.SaveChanges();
        return show.Id;
    }

    [Fact]
    public void RendersShow_WithSeasonTabsAndNextEpisode()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));

        Assert.Contains("Breaking Bad", cut.Markup);
        Assert.Contains("Season 1", cut.Markup);
        Assert.Contains("Next Episode", cut.Markup);
    }

    [Fact]
    public void CatchUp_MarksPastUnwatchedEpisodesAsWatched()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-success.btn-sm.ms-1").Click();

        using var verify = Db.CreateContext();
        var pastEpisodes = verify.Episodes.Where(e => e.AirDateOffset2 < DateTimeOffset.UtcNow);
        Assert.All(pastEpisodes, e => Assert.True(e.Watched));
    }

    [Fact]
    public void GiveUp_MarksPastUnwatchedEpisodesAsGivenUp()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-danger.btn-sm.ms-1").Click();

        using var verify = Db.CreateContext();
        var pastUnwatched = verify.Episodes.Where(e => !e.Watched && e.AirDateOffset2 < DateTimeOffset.UtcNow);
        Assert.All(pastUnwatched, e => Assert.True(e.GivenUp));
    }

    [Fact]
    public async Task SavingNotes_PersistsThroughRealService()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("textarea.form-control").Change("Great show");
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Save Notes");
        await saveButton.ClickAsync(new());

        using var verify = Db.CreateContext();
        Assert.Equal("Great show", verify.Shows.Find(showId)!.Notes);
        Assert.Contains("Saved", cut.Markup);
    }

    [Fact]
    public async Task ChangingPriority_PersistsThroughRealService()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        await cut.Find("select.form-select-sm").ChangeAsync("2");

        using var verify = Db.CreateContext();
        Assert.Equal(2, verify.Shows.Find(showId)!.Priority);
    }

    [Fact]
    public async Task AddingAndRemovingFolderAlias_PersistsThroughRealService()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-outline-secondary.btn-sm").Click(); // toggles "showAliasInput"

        await cut.Find("input[placeholder='Old show / folder name in files']").InputAsync("Old Show Name");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Add").Click();

        using (var verify = Db.CreateContext())
        {
            var alias = Assert.Single(verify.ShowFolderAliases);
            Assert.Equal("Old Show Name", alias.AliasName);
        }

        cut.Find("i.fa-times.ms-1").Click(); // remove alias

        using var verify2 = Db.CreateContext();
        Assert.Empty(verify2.ShowFolderAliases);
    }

    [Fact]
    public void AliasSearchDropdown_ShowsMatchingShows_AndSelectingFillsInFolderName()
    {
        var showId = SeedShowWithEpisodes();
        using (var ctx = Db.CreateContext())
        {
            var other = TestData.NewShow("Better Call Saul", folderName: "Better.Call.Saul");
            ctx.Shows.Add(other);
            ctx.SaveChanges();
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-outline-secondary.btn-sm").Click(); // toggles "showAliasInput"

        cut.Find("input[placeholder='Old show / folder name in files']").Input("Better");
        cut.WaitForAssertion(() => Assert.Contains("Better Call Saul", cut.Markup));
        cut.Find("div[style*='cursor: pointer']").Click(); // select search result

        Assert.Equal("Better.Call.Saul", cut.Find("input[placeholder='Old show / folder name in files']").GetAttribute("value"));
    }

    [Fact]
    public void AddingAndRemovingShowLink_PersistsThroughRealService()
    {
        var showId = SeedShowWithEpisodes();
        int otherShowId;
        using (var ctx = Db.CreateContext())
        {
            var other = TestData.NewShow("Better Call Saul");
            ctx.Shows.Add(other);
            ctx.SaveChanges();
            otherShowId = other.Id;
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.FindAll("button").First(b => b.TextContent.Contains("Link a series")).Click();

        cut.Find("input[placeholder='Search show...']").Input("Better");
        cut.WaitForAssertion(() => Assert.Contains("Better Call Saul", cut.Markup));
        cut.Find("div[style*='cursor:pointer']").Click(); // select search result

        var addButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Add" && !b.HasAttribute("disabled"));
        addButton.Click();

        using (var verify = Db.CreateContext())
        {
            var link = Assert.Single(verify.ShowLinks);
            Assert.Equal(showId, link.PredecessorShowId);
            Assert.Equal(otherShowId, link.SuccessorShowId);
        }

        cut.Find("i.fa-times.text-danger.ms-1").Click(); // remove link

        using var verify2 = Db.CreateContext();
        Assert.Empty(verify2.ShowLinks);
    }

    [Fact]
    public void SettingFolderName_OpensModalAndPersistsThroughRealService()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-primary.btn-sm").Click(); // "Set Folder Name"

        Assert.Contains("Save Folder Name", cut.Markup);

        cut.Find(".modal-body input.form-control").Change(@"Breaking Bad [2008]");
        cut.Find("button.btn-primary:not(.btn-sm)").Click(); // "Save" in modal footer

        using var verify = Db.CreateContext();
        Assert.Equal(@"Breaking Bad [2008]", verify.Shows.Find(showId)!.FolderName);
        Assert.DoesNotContain("Save Folder Name", cut.Markup);
    }

    [Fact]
    public void CancellingFolderNameModal_ClosesWithoutSaving()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-primary.btn-sm").Click(); // "Set Folder Name"
        cut.Find("button.btn-secondary").Click(); // "Cancel" in modal footer

        Assert.DoesNotContain("Save Folder Name", cut.Markup);
        using var verify = Db.CreateContext();
        Assert.Null(verify.Shows.Find(showId)!.FolderName);
    }

    [Fact]
    public void MarkingEpisodeWatchedFromSeasonTab_PersistsThroughRealService()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("#seasonTabContent i.far.fa-eye").Click();

        using var verify = Db.CreateContext();
        Assert.Contains(verify.Episodes, e => e.Watched && e.number == 2);
    }

    [Fact]
    public void MarkingWholeSeasonWatched_PersistsThroughRealService()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("#seasonTabContent i.fa-tasks").Click();

        using var verify = Db.CreateContext();
        Assert.All(verify.Episodes.Where(e => e.show!.Id == showId), e => Assert.True(e.Watched));
    }

    [Fact]
    public void ClickingTypeLanguageNetworkWebNetworkAndCountryFilters_PersistThroughRealService()
    {
        int typeId, languageId, networkId, webNetworkId, networkCountryId;
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var type = TestData.NewType("Scripted");
            var language = TestData.NewLanguage("English");
            var country = TestData.NewCountry("US");
            var network = TestData.NewNetwork("AMC", country: country);
            var webNetwork = TestData.NewWebNetwork("Netflix");
            var show = TestData.NewShow("Full Show", wanted: true,
                type: type, language: language, network: network, webNetwork: webNetwork);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
            typeId = type.Id;
            languageId = language.Id;
            networkId = network.Id;
            webNetworkId = webNetwork.Id;
            networkCountryId = country.Id;
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("i[title='Select type']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Types.Find(typeId)!.Wanted);

        cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("i[title='Select language']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Languages.Find(languageId)!.Wanted);

        cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("i[title='Select network']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Networks.Find(networkId)!.Wanted);

        cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("i[title='Select country']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.Countrys.Find(networkCountryId)!.Wanted);

        cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("i[title='Select webnetwork']").Click();
        using (var verify = Db.CreateContext())
            Assert.True(verify.WebNetworks.Find(webNetworkId)!.Wanted);
    }

    [Fact]
    public void RendersTypeLanguageNetworkWebNetworkCountriesAndSummary()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Full Show", wanted: true,
                type: TestData.NewType("Scripted"),
                language: TestData.NewLanguage("English"),
                network: TestData.NewNetwork("AMC", country: TestData.NewCountry("US")),
                webNetwork: TestData.NewWebNetwork("Netflix", country: TestData.NewCountry("GB")));
            show.summary = "A gripping drama";
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));

        Assert.Contains("Scripted", cut.Markup);
        Assert.Contains("English", cut.Markup);
        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("US)", cut.Markup);
        Assert.Contains("Netflix", cut.Markup);
        Assert.Contains("GB)", cut.Markup);
        Assert.Contains("A gripping drama", cut.Markup);
    }

    [Fact]
    public void RendersShowsWithSameName_AsPotentialConfusionWarning()
    {
        var showId = SeedShowWithEpisodes();
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad", showid: 999)); // same name, different showid
            ctx.SaveChanges();
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));

        Assert.Contains("may get confused with", cut.Markup);
    }

    [Fact]
    public void ClickingWatchedIconOnNextEpisode_Desktop_MarksItWatched()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("div.row.bg-light i.far.fa-eye").Click();

        using var verify = Db.CreateContext();
        Assert.Contains(verify.Episodes, e => e.Watched && e.number == 3);
    }

    [Fact]
    public void ClickingGiveUpIconOnNextEpisode_Desktop_MarksItGivenUp()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("div.row.bg-light i.fas.fa-flag.text-muted").Click();

        using var verify = Db.CreateContext();
        Assert.Contains(verify.Episodes, e => e.GivenUp && e.number == 3);
    }

    [Fact]
    public void ClickingWatchedIconOnNextEpisode_Mobile_MarksItWatched()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("div.d-md-none i.far.fa-eye").Click();

        using var verify = Db.CreateContext();
        Assert.Contains(verify.Episodes, e => e.Watched && e.number == 3);
    }

    [Fact]
    public void UndoingGivenUpNextEpisode_ThroughIcon_MarksItNotGivenUp()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Given Up Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(7), givenUp: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("div.row.bg-light i.fas.fa-flag.text-danger").Click();

        using var verify = Db.CreateContext();
        Assert.Contains(verify.Episodes, e => !e.GivenUp && e.number == 1);
    }

    [Fact]
    public void RendersSimilarShows_MatchingOnSharedGenre()
    {
        var genre = TestData.NewGenreText("Drama");
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Breaking Bad", wanted: true);
            show.Genres = new List<Showlist2026.Entities.Genre> { new() { genretext = genre, show = show } };
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30));

            var similar = TestData.NewShow("Better Call Saul", // undecided, so not excluded
                network: TestData.NewNetwork("AMC"), status: "Running");
            similar.imagemed = "http://img/similar.jpg";
            similar.Genres = new List<Showlist2026.Entities.Genre> { new() { genretext = genre, show = similar } };

            ctx.Shows.Add(show);
            ctx.Shows.Add(similar);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));

        Assert.Contains("Similar Shows", cut.Markup);
        Assert.Contains("Better Call Saul", cut.Markup);
        Assert.Contains("http://img/similar.jpg", cut.Markup);
        Assert.Contains("bg-secondary\">Running", cut.Markup);
    }

    [Fact]
    public void CrawlNzbSites_WithNoUnwatchedEpisodes_ReportsNoEpisodesError()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("All Watched Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30), watched: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-outline-primary.btn-sm.ms-3").Click(); // Crawl API

        Assert.Contains("Crawl Results: 0 found from 0 sites", cut.Markup);
        Assert.Contains("No unwatched episodes to search for", cut.Markup);
    }

    [Fact]
    public void CrawlRssFeeds_WithNoUnwatchedEpisodes_ReportsNoEpisodesError()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("All Watched Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30), watched: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-outline-info.btn-sm.ms-1").Click(); // Crawl RSS

        Assert.Contains("Crawl Results: 0 found from 0 sites", cut.Markup);
        Assert.Contains("No unwatched episodes to search for", cut.Markup);
    }

    [Fact]
    public async Task AddingAliasWithSeasonOffset_ShowsMappingPreviewAndPersistedBadge()
    {
        var showId = SeedShowWithEpisodes();

        var cut = Render<ShowDetail>(p => p.Add(c => c.ShowId, showId));
        cut.Find("button.btn-outline-secondary.btn-sm").Click(); // toggles "showAliasInput"

        cut.Find("input[placeholder='Season offset']").Input("2");
        Assert.Contains("S3", cut.Markup);
        Assert.Contains("this show's S01", cut.Markup);

        await cut.Find("input[placeholder='Old show / folder name in files']").InputAsync("Old Show Name");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Add").Click();

        Assert.Contains("S3", cut.Markup);
        Assert.Contains("S1", cut.Markup);
        using var verify = Db.CreateContext();
        var alias = Assert.Single(verify.ShowFolderAliases);
        Assert.Equal(2, alias.SeasonOffset);
    }
}
