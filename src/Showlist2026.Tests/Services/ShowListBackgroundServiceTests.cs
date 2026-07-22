using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListBackgroundServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public ShowListBackgroundServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "Showlist2026BgTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort cleanup */ }
    }

    [Fact]
    public void GetEstimatedPageMax_ComputesFromMaxShowId()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        ctx.Shows.Add(TestData.NewShow("Show", showid: 749));
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);

        // 749 / 250 = 2.996 -> floor 2, + 1 = 3
        Assert.Equal(3, service.GetEstimatedPageMax());
    }

    [Fact]
    public async Task Notificationtest_SendsThroughNotificationService()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var notifications = new FakeNotificationService();
        var service = TestFactory.CreateBackgroundService(ctx, notifications: notifications);

        await service.Notificationtest();

        Assert.Single(notifications.Sent);
    }

    [Fact]
    public async Task ShowDownloadedJob_MarksMatchedEpisodeWatched_AndRecordsTouchFile()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        var videoFile = Path.Combine(showFolder, "My.Show.S01E01.mkv");
        File.WriteAllBytes(videoFile, new byte[1024]);

        int showId, episodeId;
        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            var ep = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
            episodeId = ep.Id;
        }

        var notifications = new FakeNotificationService();
        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx, notifications: notifications);
            var result = await service.ShowDownloadedJob();
            Assert.True(result);
        }

        using var verify = db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
        Assert.NotNull(verify.TouchFiles.SingleOrDefault(t => t.Name == "My.Show.S01E01.mkv"));
        Assert.NotNull(verify.TouchFolder.SingleOrDefault(f => f.Name == "my.show"));
        Assert.Single(notifications.Sent);
    }

    [Fact]
    public async Task ShowDownloadedJob_AppliesFolderAliasSeasonOffset_ForContinuationShow()
    {
        // Regression coverage for the alias/continuation feature: a file under an old show's
        // folder name (e.g. a renamed/relaunched series) should resolve to the *successor*
        // show's episode at (fileSeason - SeasonOffset), not the original folder's own show.
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        // Files sit under the OLD folder name "Old.Show.Name", season 4 on disk.
        var showFolder = Path.Combine(tvDir, "Old.Show.Name", "Season 4");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "Old.Show.Name.S04E02.mkv"), new byte[1024]);

        int successorEpisodeId;
        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });

            // The successor show continues numbering from season 1 (offset 3: fileSeason 4 -> showSeason 1)
            var successor = TestData.NewShow("New Show Name", wanted: true, folderName: "New.Show.Name");
            var ep = TestData.NewEpisode(successor, 1, 2, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(successor);
            ctx.SaveChanges();

            ctx.ShowFolderAliases.Add(new ShowFolderAlias
            {
                ShowId = successor.Id,
                AliasName = "Old.Show.Name",
                SeasonOffset = 3
            });
            ctx.SaveChanges();
            successorEpisodeId = ep.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            await service.ShowDownloadedJob();
        }

        using var verify = db.CreateContext();
        Assert.True(verify.Episodes.Find(successorEpisodeId)!.Watched);
    }

    [Fact]
    public async Task ResolveAliasFolders_MergesAliasFolderIntoRealFolder_ApplyingSeasonOffset()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        Directory.CreateDirectory(tvDir);

        var aliasFolder = Path.Combine(tvDir, "Old.Show.Name");
        var aliasSeason4 = Path.Combine(aliasFolder, "Season 4");
        Directory.CreateDirectory(aliasSeason4);
        File.WriteAllBytes(Path.Combine(aliasSeason4, "episode.mkv"), new byte[10]);

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, Aliasable = true, DaysToScan = 0 });
            var show = TestData.NewShow("New Show Name", wanted: true, folderName: "New.Show.Name");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            ctx.ShowFolderAliases.Add(new ShowFolderAlias { ShowId = show.Id, AliasName = "Old.Show.Name", SeasonOffset = 3 });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            var result = await service.ResolveAliasFolders();
            Assert.True(result);
        }

        var realFolder = Path.Combine(tvDir, "New.Show.Name");
        Assert.False(Directory.Exists(aliasFolder)); // alias folder consumed/removed after merge
        Assert.True(Directory.Exists(Path.Combine(realFolder, "Season 1"))); // 4 - offset(3) = 1
        Assert.True(File.Exists(Path.Combine(realFolder, "Season 1", "episode.mkv")));
    }

    [Fact]
    public async Task ResolveAliasFolders_NoOp_WhenNoAliasableDirectoriesConfigured()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        var result = await service.ResolveAliasFolders();

        Assert.True(result);
    }
}
