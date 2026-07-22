using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListAppServiceLookupTests
{
    [Fact]
    public void Showlist_SearchesByNameSubstring_CaseSensitiveContains()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad"));
            ctx.Shows.Add(TestData.NewShow("Better Call Saul"));
            ctx.Shows.Add(TestData.NewShow("The Wire"));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.showlist("Bad");

        var result = Assert.Single(results);
        Assert.Equal("Breaking Bad", result.name);
    }

    [Fact]
    public void ShowData_ReturnsOnlyDecidedShows_SortedByName()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Zebra Show", wanted: true));
            ctx.Shows.Add(TestData.NewShow("Alpha Show", wanted: false));
            ctx.Shows.Add(TestData.NewShow("Undecided Show"));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.ShowData();

        Assert.Equal(new[] { "Alpha Show", "Zebra Show" }, results.Select(s => s.name));
    }

    [Fact]
    public void NoFolderList_ReturnsWantedShowsMissingFolderName_WithSuggestedNames()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            // Two wanted shows share a name -> SuggestedFolderName should get the year appended
            // to disambiguate them; a uniquely-named show should not.
            ctx.Shows.Add(TestData.NewShow("Duplicate Name", wanted: true, premiered: "1999-01-01"));
            ctx.Shows.Add(TestData.NewShow("Duplicate Name", wanted: true, premiered: "2020-01-01"));
            ctx.Shows.Add(TestData.NewShow("Unique Name", wanted: true));
            ctx.Shows.Add(TestData.NewShow("Has Folder Already", wanted: true, folderName: "Has.Folder.Already"));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.NoFolderList();

        Assert.Equal(3, results.Count);
        Assert.DoesNotContain(results, s => s.name == "Has Folder Already");

        var dupes = results.Where(s => s.name == "Duplicate Name").ToList();
        Assert.Equal(2, dupes.Count);
        Assert.Contains(dupes, s => s.SuggestedFolderName == "Duplicate Name 1999");
        Assert.Contains(dupes, s => s.SuggestedFolderName == "Duplicate Name 2020");

        var unique = results.Single(s => s.name == "Unique Name");
        Assert.Equal("Unique Name", unique.SuggestedFolderName);
    }

    [Fact]
    public void ShowPageData_ReturnsShowWithNavigationsLoaded()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            TestData.NewEpisode(show, 1, 1);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var result = service.ShowPageData(showId);

        Assert.NotNull(result);
        Assert.Equal("Show", result!.name);
        Assert.Single(result.Episodes!);
    }

    [Fact]
    public void ShowPageData_ReturnsNull_WhenShowMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        Assert.Null(service.ShowPageData(999));
    }

    [Fact]
    public void HomePageStats_CountsShowsEpisodesAndRecentUpdates()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var recentlyUpdated = TestData.NewShow("Recent Show");
            recentlyUpdated.needsupdate = false;
            recentlyUpdated.updated = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString();
            TestData.NewEpisode(recentlyUpdated, 1, 1, watched: true);

            var needsUpdate = TestData.NewShow("Stale Show");
            needsUpdate.needsupdate = true;

            ctx.Shows.AddRange(recentlyUpdated, needsUpdate);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var stats = service.HomePageStats();

        Assert.Equal(2, stats.shows);
        Assert.Equal(1, stats.episodes);
        Assert.Equal(1, stats.showsNeedingUpdate);
        Assert.Equal(1, stats.watchedEpisodes);
        Assert.Single(stats.recentshows);
        Assert.Equal("Recent Show", stats.recentshows[0].name);
    }
}
