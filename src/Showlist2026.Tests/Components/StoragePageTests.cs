using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class StoragePageTests : BlazorTestBase
{
    [Fact]
    public void NoDirectoriesConfigured_RendersZeroState()
    {
        var cut = Render<Storage>();

        Assert.Contains("Storage Dashboard", cut.Markup);
        Assert.Contains("0.0 KB", cut.Markup);
    }

    [Fact]
    public void ConfiguredDirectory_RendersMatchedAndUnmatchedFolders()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "Showlist2026Tests_Storage_" + Guid.NewGuid());
        var matchedShowFolder = Path.Combine(basePath, "Breaking Bad");
        var season1 = Path.Combine(matchedShowFolder, "Season 1");
        var unmatchedFolder = Path.Combine(basePath, "Unknown Show");
        Directory.CreateDirectory(season1);
        Directory.CreateDirectory(unmatchedFolder);
        File.WriteAllText(Path.Combine(season1, "Breaking.Bad.S01E01.mkv"), "some video bytes");
        File.WriteAllText(Path.Combine(unmatchedFolder, "random.mkv"), "other bytes");

        try
        {
            using (var ctx = Db.CreateContext())
            {
                ctx.TVDirectories.Add(new Showlist2026.Entities.TVDirectories { Name = basePath, DaysToScan = 7 });
                var show = TestData.NewShow("Breaking Bad", folderName: "Breaking Bad");
                ctx.Shows.Add(show);
                ctx.SaveChanges();
            }

            var cut = Render<Storage>();

            Assert.Contains("Breaking Bad", cut.Markup);
            Assert.Contains("unmatched", cut.Markup);

            cut.Find("select.form-select-sm").Change("matched");
            Assert.Contains("Breaking Bad", cut.Markup);
            Assert.DoesNotContain("Unknown Show", cut.Markup);
        }
        finally
        {
            Directory.Delete(basePath, recursive: true);
        }
    }

    [Fact]
    public void FilteringAndSorting_AndFormattingLargerSizes_WorkAcrossMultipleShows()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "Showlist2026Tests_Storage_" + Guid.NewGuid());
        var wantedFolder = Path.Combine(basePath, "Wanted Show");
        var notWantedFolder = Path.Combine(basePath, "Excluded Show");
        var unmatchedFolder = Path.Combine(basePath, "Unknown Show");
        Directory.CreateDirectory(wantedFolder);
        Directory.CreateDirectory(notWantedFolder);
        Directory.CreateDirectory(unmatchedFolder);
        File.WriteAllBytes(Path.Combine(wantedFolder, "ep.mkv"), new byte[2 * 1024 * 1024]); // 2MB, crosses the MB formatting threshold
        File.WriteAllBytes(Path.Combine(notWantedFolder, "ep.mkv"), new byte[1024]);
        File.WriteAllBytes(Path.Combine(unmatchedFolder, "ep.mkv"), new byte[512]);

        try
        {
            using (var ctx = Db.CreateContext())
            {
                ctx.TVDirectories.Add(new Showlist2026.Entities.TVDirectories { Name = basePath, DaysToScan = 7 });
                ctx.Shows.Add(TestData.NewShow("Wanted Show", wanted: true, folderName: "Wanted Show", status: "Running"));
                ctx.Shows.Add(TestData.NewShow("Excluded Show", wanted: false, folderName: "Excluded Show"));
                ctx.SaveChanges();
            }

            var cut = Render<Storage>();

            Assert.Contains("2.0 MB", cut.Markup);
            Assert.Contains("bg-success\">Running", cut.Markup);

            cut.FindAll("select.form-select-sm")[0].Change("unmatched");
            var rows = cut.FindAll("tbody tr");
            Assert.Single(rows);
            Assert.Contains("unmatched", rows[0].TextContent);

            cut.FindAll("select.form-select-sm")[0].Change("wanted");
            rows = cut.FindAll("tbody tr");
            Assert.Single(rows);
            Assert.Contains("Wanted Show", rows[0].TextContent);

            cut.FindAll("select.form-select-sm")[0].Change("notwanted");
            rows = cut.FindAll("tbody tr");
            Assert.Single(rows);
            Assert.Contains("Excluded Show", rows[0].TextContent);

            cut.FindAll("select.form-select-sm")[0].Change("all");
            cut.FindAll("select.form-select-sm")[1].Change("name");
            cut.FindAll("select.form-select-sm")[1].Change("files");
            cut.FindAll("select.form-select-sm")[1].Change("smallest");
            Assert.Equal(3, cut.FindAll("tbody tr").Count);
        }
        finally
        {
            Directory.Delete(basePath, recursive: true);
        }
    }
}
