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
}
