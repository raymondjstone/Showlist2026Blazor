using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListAppServiceImportExportTests
{
    [Fact]
    public async Task ExportImport_RoundTripsShowSelectionsAndWatchedEpisodes()
    {
        using var sourceDb = new TestDb();
        using (var ctx = sourceDb.CreateContext())
        {
            var show = TestData.NewShow("Show", showid: 555, wanted: true, folderName: "Show Folder");
            TestData.NewEpisode(show, 1, 1, watched: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var sourceService = TestFactory.CreateAppService(sourceDb);
        var json = sourceService.ExportUserDataAsJson();

        Assert.Contains("555", json);
        Assert.Contains("Show Folder", json);

        using var destDb = new TestDb();
        using (var ctx = destDb.CreateContext())
        {
            var show = TestData.NewShow("Show", showid: 555); // undecided, no folder yet
            TestData.NewEpisode(show, 1, 1);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var destService = TestFactory.CreateAppService(destDb);
        var imported = await destService.ImportUserDataFromJson(json);

        Assert.Equal(2, imported); // 1 show selection + 1 watched episode

        using var verify = destDb.CreateContext();
        var importedShow = verify.Shows.Single(s => s.showid == 555);
        Assert.True(importedShow.Wanted);
        Assert.Equal("Show Folder", importedShow.FolderName);
        Assert.True(verify.Episodes.Single(e => e.show!.Id == importedShow.Id).Watched);
    }

    [Fact]
    public async Task Import_DoesNotOverwriteExistingDecisions()
    {
        using var destDb = new TestDb();
        using (var ctx = destDb.CreateContext())
        {
            var show = TestData.NewShow("Show", showid: 42, wanted: false, folderName: "Existing Folder");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var json = """
        {
          "ExportDate": "2024-01-01T00:00:00Z",
          "ShowSelections": [
            { "TvMazeShowId": 42, "ShowName": "Show", "Include": true, "FolderName": "Imported Folder" }
          ],
          "WatchedEpisodes": []
        }
        """;

        var service = TestFactory.CreateAppService(destDb);
        var imported = await service.ImportUserDataFromJson(json);

        Assert.Equal(0, imported); // already decided (Wanted=false) and already has a folder name

        using var verify = destDb.CreateContext();
        var show2 = verify.Shows.Single(s => s.showid == 42);
        Assert.False(show2.Wanted);
        Assert.Equal("Existing Folder", show2.FolderName);
    }

    [Fact]
    public void ExportUserDataAsCsv_IncludesShowSelectionsAndWatchedEpisodes()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("CSV Show", showid: 77, wanted: true);
            TestData.NewEpisode(show, 1, 1, watched: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var csv = service.ExportUserDataAsCsv();

        Assert.Contains("ShowSelection,77,\"CSV Show\"", csv);
        Assert.Contains("Watched,77,\"CSV Show\",1,1", csv);
    }

    [Fact]
    public void PreviewImportWatchedFromPaths_MatchesFolderAndComputesEpisodeRange()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            // ParseImportPaths only matches shows premiered at/before 2015 (legacy watch-history
            // import cutover) - premiered must be set for the show to land in its folder lookup.
            var show = TestData.NewShow("My Show", folderName: "My.Show", premiered: "2010-01-01");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var fileContent = string.Join("\n", new[]
        {
            @"D:\TV\My.Show\Season 1\My.Show.S01E01.mkv",
            @"D:\TV\My.Show\Season 1\My.Show.S01E02.mkv",
            @"D:\TV\My.Show\Season 2\My.Show.S02E01.mkv",
            @"D:\TV\Unmatched.Show\Season 1\Unmatched.Show.S01E01.mkv",
            @"D:\TV\My.Show\Season 1\readme.txt", // not a video extension, skipped
        });

        var service = TestFactory.CreateAppService(db);
        var preview = service.PreviewImportWatchedFromPaths(fileContent);

        var match = Assert.Single(preview.MatchedShows);
        Assert.Equal("My Show", match.ShowName);
        Assert.Equal(3, match.EpisodeCount);
        Assert.Equal("Up to S02E01", match.EpisodeRange);
        Assert.Contains("Unmatched.Show", preview.UnmatchedFolders);
    }

    [Fact]
    public void PreviewImportWatchedFromPaths_FallsBackToDefaultFolderName_WhenFolderNameNotSet()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            // No FolderName set - ParseImportPaths must fall back to DefaultFolderName ("My Show").
            var show = TestData.NewShow("My Show", premiered: "2010-01-01");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var fileContent = @"D:\TV\My Show\Season 1\My.Show.S01E01.mkv";

        var service = TestFactory.CreateAppService(db);
        var preview = service.PreviewImportWatchedFromPaths(fileContent);

        var match = Assert.Single(preview.MatchedShows);
        Assert.Equal("My Show", match.ShowName);
    }

    [Fact]
    public async Task CommitImportWatchedFromPaths_MarksShowWantedAndEpisodesUpToMaxWatched()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            // ParseImportPaths only matches shows premiered at/before 2015 (legacy watch-history
            // import cutover) - premiered must be set for the show to land in its folder lookup.
            var show = TestData.NewShow("My Show", folderName: "My.Show", premiered: "2010-01-01");
            TestData.NewEpisode(show, 1, 1);
            TestData.NewEpisode(show, 1, 2);
            TestData.NewEpisode(show, 2, 1);
            TestData.NewEpisode(show, 2, 2); // beyond the imported max, should stay unwatched
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var fileContent = string.Join("\n", new[]
        {
            @"D:\TV\My.Show\Season 1\My.Show.S01E01.mkv",
            @"D:\TV\My.Show\Season 2\My.Show.S02E01.mkv",
        });

        var service = TestFactory.CreateAppService(db);
        var (showsMatched, episodesMarked) = await service.CommitImportWatchedFromPaths(fileContent);

        Assert.Equal(1, showsMatched);
        Assert.Equal(3, episodesMarked); // S01E01, S01E02 (season < max season), S02E01

        using var verify = db.CreateContext();
        Assert.True(verify.Shows.Find(showId)!.Wanted);
        var eps = verify.Episodes.Where(e => e.show!.Id == showId).ToDictionary(e => (e.season, e.number), e => e.Watched);
        Assert.True(eps[(1, 1)]);
        Assert.True(eps[(1, 2)]);
        Assert.True(eps[(2, 1)]);
        Assert.False(eps[(2, 2)]); // beyond max imported episode
    }

    [Fact]
    public void PreviewImportWatchedFromPaths_SkipsShowsAlreadyWanted()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("My Show", folderName: "My.Show", wanted: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var fileContent = @"D:\TV\My.Show\Season 1\My.Show.S01E01.mkv";

        var service = TestFactory.CreateAppService(db);
        var preview = service.PreviewImportWatchedFromPaths(fileContent);

        Assert.Empty(preview.MatchedShows);
    }
}
