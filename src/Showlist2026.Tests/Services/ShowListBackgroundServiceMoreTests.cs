using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListBackgroundServiceMoreTests : IDisposable
{
    private readonly string _tempRoot;

    public ShowListBackgroundServiceMoreTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "Showlist2026BgMoreTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort cleanup */ }
    }

    [Fact]
    public void HomePageStats_CountsShowsAndWatchedEpisodes()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var show = TestData.NewShow("Show");
        TestData.NewEpisode(show, 1, 1, watched: true);
        TestData.NewEpisode(show, 1, 2);
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        var stats = service.HomePageStats();

        Assert.Equal(1, stats.shows);
        Assert.Equal(2, stats.episodes);
        Assert.Equal(1, stats.watchedEpisodes);
    }

    [Fact]
    public async Task BacklogPage_ReturnsFalse_WhenNoShowNeedsUpdate()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var show = TestData.NewShow("Show");
        show.needsupdate = false;
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.False(await service.BacklogPage());
    }

    [Fact]
    public async Task RecheckTouchFiles_IsANoOpThatSucceeds()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.RecheckTouchFiles());
    }

    [Fact]
    public async Task PopulateShowFolderNames_FindsFolderMatchingDefaultFolderName()
    {
        // rootfolder is concatenated directly with the attempted folder name (no separator
        // inserted by production code), so it must itself end with a directory separator.
        var rootfolder = _tempRoot + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(_tempRoot, "My Show"));

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx, TestFactory.Options(tvNameListPath: rootfolder));
            var result = await service.PopulateShowFolderNames();
            Assert.True(result);
        }

        using var verify = db.CreateContext();
        Assert.Equal("My Show", verify.Shows.Find(showId)!.FolderName);
    }

    [Fact]
    public async Task PopulateShowFolderNames_FindsFolderMatchingDefaultNamePlusNetworkCountryCode()
    {
        // networklist/webnetworklist are loaded as ALL networks/web-networks in the DB (not
        // specifically the show's own), and every country code among them is tried as a
        // "Name XX" suffix - so a matching Network need not be assigned to the show at all.
        var rootfolder = _tempRoot + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(_tempRoot, "My Show US"));

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            var network = TestData.NewNetwork("Some Network", country: TestData.NewCountry("US"));
            ctx.Shows.Add(show);
            ctx.Networks.Add(network);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx, TestFactory.Options(tvNameListPath: rootfolder));
            await service.PopulateShowFolderNames();
        }

        using var verify = db.CreateContext();
        Assert.Equal("My Show US", verify.Shows.Find(showId)!.FolderName);
    }

    [Fact]
    public async Task PopulateShowFolderNames_FindsFolderMatchingDefaultNamePlusWebNetworkCountryCode()
    {
        var rootfolder = _tempRoot + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(_tempRoot, "My Show GB"));

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            var webNetwork = TestData.NewWebNetwork("Some Web Network", country: TestData.NewCountry("GB"));
            ctx.Shows.Add(show);
            ctx.WebNetworks.Add(webNetwork);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx, TestFactory.Options(tvNameListPath: rootfolder));
            await service.PopulateShowFolderNames();
        }

        using var verify = db.CreateContext();
        Assert.Equal("My Show GB", verify.Shows.Find(showId)!.FolderName);
    }

    [Fact]
    public async Task PopulateShowFolderNames_FindsFolderMatchingDefaultNamePlusPremiereYear()
    {
        var rootfolder = _tempRoot + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(_tempRoot, "My Show 2015"));

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true, premiered: "2015-03-01");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx, TestFactory.Options(tvNameListPath: rootfolder));
            await service.PopulateShowFolderNames();
        }

        using var verify = db.CreateContext();
        Assert.Equal("My Show 2015", verify.Shows.Find(showId)!.FolderName);
    }

    [Fact]
    public async Task PopulateShowFolderNames_LeavesFolderNameNull_WhenNoMatchingDirectoryExists()
    {
        var rootfolder = _tempRoot + Path.DirectorySeparatorChar;

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("No Matching Folder", wanted: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx, TestFactory.Options(tvNameListPath: rootfolder));
            await service.PopulateShowFolderNames();
        }

        using var verify = db.CreateContext();
        Assert.Null(verify.Shows.Find(showId)!.FolderName);
    }

    [Fact]
    public async Task ScanDirectoryFull_MatchesEpisodeAndMarksWatched()
    {
        var scanDir = Path.Combine(_tempRoot, "ScanMe");
        var showFolder = Path.Combine(scanDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E01.mkv"), new byte[10]);

        using var db = new TestDb();
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
            var service = TestFactory.CreateBackgroundService(ctx);
            var result = await service.ScanDirectoryFull(scanDir);
            Assert.True(result);
        }

        using var verify = db.CreateContext();
        Assert.True(verify.Episodes.Find(episodeId)!.Watched);
    }

    [Fact]
    public async Task ScanDirectoryFull_Throws_WhenDirectoryDoesNotExist()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => service.ScanDirectoryFull(Path.Combine(_tempRoot, "does-not-exist")));
    }
}
