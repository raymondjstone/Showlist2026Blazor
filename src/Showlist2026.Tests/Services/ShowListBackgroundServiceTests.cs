using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Showlist2026.Entities;
using Showlist2026.Services;
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
    public async Task ShowDownloadedJob_SkipsContinuationCandidate_WhenSeasonOffsetProducesInvalidSeason()
    {
        // effectiveSeason = fileSeason(1) - SeasonOffset(5) = -4, which ResolveShowEpisode must
        // reject and keep scanning rather than matching (or crashing on) a negative season.
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "Old.Show.Name", "Season 1");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "Old.Show.Name.S01E01.mkv"), new byte[1024]);

        int episodeId;
        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
            var successor = TestData.NewShow("New Show Name", wanted: true, folderName: "New.Show.Name");
            var ep = TestData.NewEpisode(successor, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(successor);
            ctx.SaveChanges();
            ctx.ShowFolderAliases.Add(new ShowFolderAlias { ShowId = successor.Id, AliasName = "Old.Show.Name", SeasonOffset = 5 });
            ctx.SaveChanges();
            episodeId = ep.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ShowDownloadedJob());
        }

        using var verify = db.CreateContext();
        Assert.False(verify.Episodes.Find(episodeId)!.Watched);
    }

    [Fact]
    public async Task ShowDownloadedJob_SwallowsAndLogs_WhenAFriendCopyFails()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E01.mkv"), new byte[1024]);

        var friendFolder = Path.Combine(_tempRoot, "FriendFolder");
        // Pre-create a FILE at the exact path the code needs as a DIRECTORY (friend/Show/Season 1),
        // so Directory.CreateDirectory(destDir) throws IOException and is caught per-file.
        Directory.CreateDirectory(friendFolder);
        File.WriteAllBytes(Path.Combine(friendFolder, "My.Show"), new byte[] { 0 });

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();

            var friend = new Friend { Name = "Alice", FolderPath = friendFolder };
            ctx.Friends.Add(friend);
            ctx.SaveChanges();
            ctx.FriendShows.Add(new FriendShow { FriendId = friend.Id, ShowId = show.Id });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ShowDownloadedJob());
        }

        using var verify = db.CreateContext();
        Assert.Empty(verify.FriendCopies);
    }

    [Fact]
    public async Task ScanDirectoryFull_UpdatesExistingTouchFile_WhenWasRealFileFlagChanges()
    {
        using var db = new TestDb();
        var scanDir = Path.Combine(_tempRoot, "ScanMe");
        var showFolder = Path.Combine(scanDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E01.mkv"), new byte[1024]);

        using (var ctx = db.CreateContext())
        {
            ctx.TouchFiles.Add(new TouchFile { Name = "My.Show.S01E01.mkv", WasRealFile = false, FileDate = DateTime.UtcNow });
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ScanDirectoryFull(scanDir));
        }

        using var verify = db.CreateContext();
        Assert.True(verify.TouchFiles.Single(t => t.Name == "My.Show.S01E01.mkv").WasRealFile);
    }

    [Fact]
    public async Task ScanDirectoryFull_StillMarksEpisodeWatched_WhenNotificationSendFails()
    {
        using var db = new TestDb();
        var scanDir = Path.Combine(_tempRoot, "ScanMe");
        var showFolder = Path.Combine(scanDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E01.mkv"), new byte[1024]);

        int episodeId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            var ep = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            episodeId = ep.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = new ShowListBackgroundService(
                ctx,
                NullLogger<ShowListBackgroundService>.Instance,
                Options.Create(TestFactory.Options()),
                new ThrowingNotificationService());
            Assert.True(await service.ScanDirectoryFull(scanDir));
        }

        using var verify = db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
    }

    [Fact]
    public async Task ShowDownloadedJob_SwallowsAndLogs_WhenAConfiguredTvDirectoryDoesNotExist()
    {
        // Dirlist's own try/catch (Directory.GetFiles on a missing path) rather than the outer
        // ShowDownloadedJob scan catch, since the directory Name itself is valid (non-null).
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        ctx.TVDirectories.Add(new TVDirectories
        {
            Name = Path.Combine(_tempRoot, "does-not-exist"),
            DaysToScan = -1,
            MinFileSize = 0,
            Filter = "*.*"
        });
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        Assert.True(await service.ShowDownloadedJob());
    }

    [Fact]
    public async Task ShowDownloadedJob_CopiesNewFileToInterestedFriendsFolder()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        var videoFile = Path.Combine(showFolder, "My.Show.S01E01.mkv");
        File.WriteAllBytes(videoFile, new byte[1024]);

        var friendFolder = Path.Combine(_tempRoot, "FriendFolder");
        int showId;
        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;

            var friend = new Friend { Name = "Alice", FolderPath = friendFolder };
            ctx.Friends.Add(friend);
            ctx.SaveChanges();
            ctx.FriendShows.Add(new FriendShow { FriendId = friend.Id, ShowId = showId });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            await service.ShowDownloadedJob();
        }

        var destFile = Path.Combine(friendFolder, "My.Show", "Season 1", "My.Show.S01E01.mkv");
        Assert.True(File.Exists(destFile));

        using var verify = db.CreateContext();
        Assert.Single(verify.FriendCopies.Where(c => c.FileName == "My.Show.S01E01.mkv"));
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

    [Fact]
    public async Task ResolveAliasFolders_NoOp_WhenNoAliasesAreDefined()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        ctx.TVDirectories.Add(new TVDirectories { Name = _tempRoot, Aliasable = true, DaysToScan = 0 });
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.ResolveAliasFolders());
    }

    [Fact]
    public async Task ResolveAliasFolders_SkipsDirectory_WhenConfiguredPathDoesNotExist()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        ctx.TVDirectories.Add(new TVDirectories { Name = Path.Combine(_tempRoot, "does-not-exist"), Aliasable = true, DaysToScan = 0 });
        var show = TestData.NewShow("New Show Name", wanted: true, folderName: "New.Show.Name");
        ctx.Shows.Add(show);
        ctx.SaveChanges();
        ctx.ShowFolderAliases.Add(new ShowFolderAlias { ShowId = show.Id, AliasName = "Old.Show.Name", SeasonOffset = 0 });
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.ResolveAliasFolders());
    }

    [Fact]
    public async Task ResolveAliasFolders_LogsAndContinues_WhenAnAliasFailsToResolve()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        Directory.CreateDirectory(tvDir);
        Directory.CreateDirectory(Path.Combine(tvDir, "Old.Show.Name"));

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, Aliasable = true, DaysToScan = 0 });
            // A colon is an invalid Windows path character - Directory.CreateDirectory(realPath)
            // throws, exercising the per-alias catch without aborting the whole run.
            var show = TestData.NewShow("Bad Name", wanted: true, folderName: "Bad:Name");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            ctx.ShowFolderAliases.Add(new ShowFolderAlias { ShowId = show.Id, AliasName = "Old.Show.Name", SeasonOffset = 0 });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ResolveAliasFolders());
        }

        // The alias folder is left untouched since the merge attempt failed.
        Assert.True(Directory.Exists(Path.Combine(tvDir, "Old.Show.Name")));
    }

    [Fact]
    public async Task ResolveAliasFolders_SkipsConflictingFile_AndLeavesAliasFolderInPlace()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var aliasFolder = Path.Combine(tvDir, "Old.Show.Name");
        var realFolder = Path.Combine(tvDir, "New.Show.Name");
        Directory.CreateDirectory(aliasFolder);
        Directory.CreateDirectory(realFolder);
        // Same file name already present at the destination - MergeDirectory must skip it rather
        // than overwrite, which also leaves the alias folder non-empty (so it isn't deleted).
        File.WriteAllBytes(Path.Combine(aliasFolder, "dupe.txt"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(realFolder, "dupe.txt"), new byte[] { 2 });

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, Aliasable = true, DaysToScan = 0 });
            var show = TestData.NewShow("New Show Name", wanted: true, folderName: "New.Show.Name");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            ctx.ShowFolderAliases.Add(new ShowFolderAlias { ShowId = show.Id, AliasName = "Old.Show.Name", SeasonOffset = 0 });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ResolveAliasFolders());
        }

        Assert.True(Directory.Exists(aliasFolder)); // not deleted - the conflicting file is still there
        Assert.Equal(new byte[] { 2 }, File.ReadAllBytes(Path.Combine(realFolder, "dupe.txt"))); // untouched
    }

    [Fact]
    public async Task ResolveAliasFolders_SkipsSeasonFolder_WhenOffsetProducesInvalidSeasonNumber()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var aliasFolder = Path.Combine(tvDir, "Old.Show.Name");
        var aliasSeason1 = Path.Combine(aliasFolder, "Season 1");
        Directory.CreateDirectory(aliasSeason1);
        File.WriteAllBytes(Path.Combine(aliasSeason1, "episode.mkv"), new byte[10]);

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, Aliasable = true, DaysToScan = 0 });
            var show = TestData.NewShow("New Show Name", wanted: true, folderName: "New.Show.Name");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            // Offset(5) applied to Season 1 -> mapped season -4, which is invalid and must be skipped.
            ctx.ShowFolderAliases.Add(new ShowFolderAlias { ShowId = show.Id, AliasName = "Old.Show.Name", SeasonOffset = 5 });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ResolveAliasFolders());
        }

        // "Season 1" was skipped (never moved), which also leaves the alias folder non-empty.
        Assert.True(Directory.Exists(aliasSeason1));
        Assert.True(File.Exists(Path.Combine(aliasSeason1, "episode.mkv")));
    }

    [Fact]
    public async Task ResolveAliasFolders_RecursivelyMerges_WhenSeasonFolderAlreadyExistsAtDestination()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var aliasFolder = Path.Combine(tvDir, "Old.Show.Name");
        var realFolder = Path.Combine(tvDir, "New.Show.Name");
        var aliasSeason1 = Path.Combine(aliasFolder, "Season 1");
        var realSeason1 = Path.Combine(realFolder, "Season 1");
        Directory.CreateDirectory(aliasSeason1);
        Directory.CreateDirectory(realSeason1);
        File.WriteAllBytes(Path.Combine(aliasSeason1, "from-alias.mkv"), new byte[10]);
        File.WriteAllBytes(Path.Combine(realSeason1, "already-there.mkv"), new byte[10]);

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, Aliasable = true, DaysToScan = 0 });
            var show = TestData.NewShow("New Show Name", wanted: true, folderName: "New.Show.Name");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            ctx.ShowFolderAliases.Add(new ShowFolderAlias { ShowId = show.Id, AliasName = "Old.Show.Name", SeasonOffset = 0 });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ResolveAliasFolders());
        }

        Assert.False(Directory.Exists(aliasFolder)); // fully merged away
        Assert.True(File.Exists(Path.Combine(realSeason1, "from-alias.mkv")));
        Assert.True(File.Exists(Path.Combine(realSeason1, "already-there.mkv")));
    }

    [Fact]
    public async Task ShowDownloadedJob_SwallowsAndLogs_WhenATvDirectoryEntryIsMalformed()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        // A null Name blows up `tvdir.Name.Trim()` before any per-directory try/catch, which
        // should be caught by the outer scan try/catch rather than failing the whole job.
        ctx.TVDirectories.Add(new TVDirectories { Name = null, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        Assert.True(await service.ShowDownloadedJob());
    }

    [Fact]
    public async Task ShowDownloadedJob_UpdatesExistingTouchFile_WhenWasRealFileFlagChanges()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        var videoFile = Path.Combine(showFolder, "My.Show.S01E01.mkv");
        File.WriteAllBytes(videoFile, new byte[1024]); // real content this time, >200 bytes

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
            // Previously recorded as a placeholder/touch file (not "real").
            ctx.TouchFiles.Add(new TouchFile { Name = "My.Show.S01E01.mkv", WasRealFile = false, FileDate = DateTime.UtcNow });
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ShowDownloadedJob());
        }

        using var verify = db.CreateContext();
        Assert.True(verify.TouchFiles.Single(t => t.Name == "My.Show.S01E01.mkv").WasRealFile);
    }

    [Fact]
    public async Task ShowDownloadedJob_LogsNoEpisodeMatch_WhenParsedSeasonEpisodeHasNoDbRow()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        // Parses fine as S01E09, but no such episode exists in the DB for this show.
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E09.mkv"), new byte[1024]);

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ShowDownloadedJob());
        }

        using var verify = db.CreateContext();
        Assert.NotNull(verify.TouchFiles.SingleOrDefault(t => t.Name == "My.Show.S01E09.mkv"));
        Assert.False(verify.Episodes.Single().Watched); // the only real episode is untouched
    }

    [Fact]
    public async Task ShowDownloadedJob_LogsUnparseableFileName_ButStillRecordsTouchFile()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "not-an-episode-name.mkv"), new byte[1024]);

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.ShowDownloadedJob());
        }

        using var verify = db.CreateContext();
        Assert.NotNull(verify.TouchFiles.SingleOrDefault(t => t.Name == "not-an-episode-name.mkv"));
    }

    private sealed class ThrowingNotificationService : Showlist2026.Services.INotificationService
    {
        public Task SendAsync(string title, string message) => throw new InvalidOperationException("notification transport is down");
        public Task<(bool success, string error)> TestPushoverAsync() => Task.FromResult((true, ""));
        public Task<(bool success, string error)> TestDiscordAsync() => Task.FromResult((true, ""));
        public Task<(bool success, string error)> TestEmailAsync() => Task.FromResult((true, ""));
    }

    [Fact]
    public async Task ShowDownloadedJob_StillMarksEpisodeWatched_WhenNotificationSendFails()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E01.mkv"), new byte[1024]);

        int episodeId;
        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = -1, MinFileSize = 0, Filter = "*.*" });
            var show = TestData.NewShow("My Show", wanted: true, folderName: "My.Show");
            var ep = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            episodeId = ep.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = new ShowListBackgroundService(
                ctx,
                NullLogger<ShowListBackgroundService>.Instance,
                Options.Create(TestFactory.Options()),
                new ThrowingNotificationService());
            Assert.True(await service.ShowDownloadedJob());
        }

        using var verify = db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
    }

    [Fact]
    public async Task ScanDirectoryFull_Throws_WhenDirectoryPathIsBlank()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ScanDirectoryFull("   "));
    }
}
