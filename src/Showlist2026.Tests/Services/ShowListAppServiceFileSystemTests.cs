using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

/// <summary>Uses real temp directories on disk since these methods scan the filesystem directly.</summary>
public class ShowListAppServiceFileSystemTests : IDisposable
{
    private readonly string _tempRoot;

    public ShowListAppServiceFileSystemTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "Showlist2026Tests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort cleanup */ }
    }

    [Fact]
    public void DeleteFile_RefusesPathsOutsideConfiguredTvDirectories()
    {
        // Regression test for the security fix: DeleteFile used to delete any path handed to it
        // (from the Dedupe UI) with no allow-list, so a malicious/incorrect path could delete
        // arbitrary files. It must now refuse anything outside a configured TV directory.
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        Directory.CreateDirectory(tvDir);
        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = 1 });
            ctx.SaveChanges();
        }

        var outsideFile = Path.Combine(_tempRoot, "OutsideTvDir", "important.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outsideFile)!);
        File.WriteAllText(outsideFile, "do not delete");

        var service = TestFactory.CreateAppService(db);
        var result = service.DeleteFile(outsideFile);

        Assert.False(result);
        Assert.True(File.Exists(outsideFile));
    }

    [Fact]
    public void DeleteFile_AllowsPathsInsideConfiguredTvDirectory()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        Directory.CreateDirectory(tvDir);
        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = 1 });
            ctx.SaveChanges();
        }

        var insideFile = Path.Combine(tvDir, "Show", "episode.mkv");
        Directory.CreateDirectory(Path.GetDirectoryName(insideFile)!);
        File.WriteAllText(insideFile, "video data");

        var service = TestFactory.CreateAppService(db);
        var result = service.DeleteFile(insideFile);

        Assert.True(result);
        Assert.False(File.Exists(insideFile));
    }

    [Fact]
    public void DeleteFile_ReturnsFalse_WhenFileDoesNotExist()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        Assert.False(service.DeleteFile(Path.Combine(_tempRoot, "does-not-exist.mkv")));
    }

    [Fact]
    public void FindExistingFolders_MatchesFolderByNameAndYearVariant()
    {
        // FindExistingFolders derives the "(year)" variant FROM an already year-suffixed folder
        // name candidate (bare "Name 2020" -> also try "Name" and "Name (2020)") - it doesn't
        // derive the year from show.premiered/ShowStart itself. So FolderName must carry the
        // bare "Name 2020" form for the parenthesised variant to be generated and matched.
        using var db = new TestDb();
        Directory.CreateDirectory(Path.Combine(_tempRoot, "My Show (2020)"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, "Unrelated Folder"));

        var service = TestFactory.CreateAppService(db, TestFactory.Options(tvNameListPath: _tempRoot));
        var show = new Show { name = "My Show", premiered = "2020-01-01", FolderName = "My Show 2020" };

        var results = service.FindExistingFolders(show, new List<ShowFolderAlias>());

        var match = Assert.Single(results);
        Assert.Equal("My Show (2020)", match.FolderName);
    }

    [Fact]
    public void FindExistingFolders_MatchesViaAlias()
    {
        using var db = new TestDb();
        Directory.CreateDirectory(Path.Combine(_tempRoot, "Old Show Name"));

        var service = TestFactory.CreateAppService(db, TestFactory.Options(tvNameListPath: _tempRoot));
        var show = new Show { name = "New Show Name" };
        var aliases = new List<ShowFolderAlias> { new() { AliasName = "Old Show Name" } };

        var results = service.FindExistingFolders(show, aliases);

        var match = Assert.Single(results);
        Assert.Equal("Old Show Name", match.FolderName);
    }

    [Fact]
    public void FindExistingFolders_ReturnsEmpty_WhenRootDoesNotExist()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db, TestFactory.Options(tvNameListPath: Path.Combine(_tempRoot, "does-not-exist")));

        var results = service.FindExistingFolders(new Show { name = "Anything" }, new List<ShowFolderAlias>());

        Assert.Empty(results);
    }

    [Fact]
    public void FindExistingFolders_MatchesFolderAlreadyInParenthesisedYearFormat()
    {
        // Complements FindExistingFolders_MatchesFolderByNameAndYearVariant: here the FolderName
        // itself is ALREADY in "(year)" form, exercising the yearRegexParen branch directly
        // rather than deriving it from a bare "Name year" candidate.
        using var db = new TestDb();
        Directory.CreateDirectory(Path.Combine(_tempRoot, "My Show 2020"));

        var service = TestFactory.CreateAppService(db, TestFactory.Options(tvNameListPath: _tempRoot));
        var show = new Show { name = "My Show", FolderName = "My Show (2020)" };

        var results = service.FindExistingFolders(show, new List<ShowFolderAlias>());

        var match = Assert.Single(results);
        Assert.Equal("My Show 2020", match.FolderName);
    }

    [Fact]
    public void FindExistingFolders_AlsoScansConfiguredTvDirectories_AndDedupesSharedRoot()
    {
        using var db = new TestDb();
        Directory.CreateDirectory(Path.Combine(_tempRoot, "My Show"));
        using (var ctx = db.CreateContext())
        {
            // Same physical root as TvNameListPath - exercises the "already seen" dedupe branch.
            ctx.TVDirectories.Add(new TVDirectories { Name = _tempRoot, DaysToScan = 7 });
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db, TestFactory.Options(tvNameListPath: _tempRoot));
        var show = new Show { name = "My Show" };

        var results = service.FindExistingFolders(show, new List<ShowFolderAlias>());

        Assert.Single(results);
    }

    [Fact]
    public void FindExistingFolders_ComputesEarliestAndLatestEpisodeFromFolderContents()
    {
        using var db = new TestDb();
        var showFolder = Path.Combine(_tempRoot, "My Show");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E01.mkv"), new byte[10]);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S02E05.mkv"), new byte[10]);
        File.WriteAllBytes(Path.Combine(showFolder, "readme.txt"), new byte[10]); // unparseable, skipped

        var service = TestFactory.CreateAppService(db, TestFactory.Options(tvNameListPath: _tempRoot));
        var show = new Show { name = "My Show" };

        var match = Assert.Single(service.FindExistingFolders(show, new List<ShowFolderAlias>()));

        Assert.Equal("S01E01", match.EarliestEpisode);
        Assert.Equal("S02E05", match.LatestEpisode);
    }

    [Fact]
    public void GetStorageDashboard_MatchesFoldersToShowsAndFlagsUnmatched()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var matchedShowFolder = Path.Combine(tvDir, "My.Show");
        var unmatchedFolder = Path.Combine(tvDir, "Random.Folder");
        Directory.CreateDirectory(matchedShowFolder);
        Directory.CreateDirectory(unmatchedFolder);
        File.WriteAllBytes(Path.Combine(matchedShowFolder, "ep1.mkv"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(unmatchedFolder, "ep1.mkv"), new byte[2048]);

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = 1 });
            ctx.Shows.Add(TestData.NewShow("My Show", wanted: true, folderName: "My.Show"));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var dashboard = service.GetStorageDashboard();

        Assert.Equal(2, dashboard.TotalFolders);
        Assert.Equal(1, dashboard.MatchedFolders);
        Assert.Equal(1, dashboard.UnmatchedFolders);
        Assert.Contains("Random.Folder", dashboard.UnmatchedFolderNames);

        var matched = dashboard.Shows.Single(s => s.FolderName == "My.Show");
        Assert.Equal("My Show", matched.ShowName);
        Assert.Equal(1024, matched.SizeBytes);
        Assert.True(matched.IsWanted);
    }

    [Fact]
    public void FindDuplicateEpisodeFiles_GroupsSameEpisodeAcrossFiles()
    {
        using var db = new TestDb();
        var tvDir = Path.Combine(_tempRoot, "TvDir");
        var showFolder = Path.Combine(tvDir, "My.Show", "Season 1");
        Directory.CreateDirectory(showFolder);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E01.720p.mkv"), new byte[1000]);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E01.1080p.mkv"), new byte[2000]);
        File.WriteAllBytes(Path.Combine(showFolder, "My.Show.S01E02.mkv"), new byte[1000]); // no duplicate

        using (var ctx = db.CreateContext())
        {
            ctx.TVDirectories.Add(new TVDirectories { Name = tvDir, DaysToScan = 1, Filter = "*.*" });
            ctx.Shows.Add(TestData.NewShow("My Show", wanted: true, folderName: "My.Show"));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var duplicates = service.FindDuplicateEpisodeFiles();

        Assert.Equal(2, duplicates.Count);
        Assert.All(duplicates, d => Assert.Equal(1, d.Season));
        Assert.All(duplicates, d => Assert.Equal(1, d.Episode));
        // Larger file first (ThenByDescending FileSize)
        Assert.Equal(2000, duplicates[0].FileSize);
        Assert.Equal(1000, duplicates[1].FileSize);
    }

    [Fact]
    public async Task Dirlist_FiltersByAgeAndSize_AndOrdersNewestFirst()
    {
        using var db = new TestDb();

        var oldFile = Path.Combine(_tempRoot, "old.mkv");
        var tooSmallFile = Path.Combine(_tempRoot, "small.mkv");
        var newerFile = Path.Combine(_tempRoot, "newer.mkv");
        var olderMatchFile = Path.Combine(_tempRoot, "older_match.mkv");

        File.WriteAllBytes(oldFile, new byte[100_000]);
        File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-30));

        File.WriteAllBytes(tooSmallFile, new byte[10]);
        File.SetLastWriteTime(tooSmallFile, DateTime.Now);

        File.WriteAllBytes(newerFile, new byte[100_000]);
        File.SetLastWriteTime(newerFile, DateTime.Now);

        File.WriteAllBytes(olderMatchFile, new byte[100_000]);
        File.SetLastWriteTime(olderMatchFile, DateTime.Now.AddDays(-1));

        var service = TestFactory.CreateAppService(db);
        var results = await service.Dirlist(_tempRoot, daysOldToAllow: 5, minSizeAllowed: 50_000);

        Assert.Equal(2, results.Count);
        Assert.Equal("newer.mkv", results[0].Name); // newest first
        Assert.Equal("older_match.mkv", results[1].Name);
    }
}
