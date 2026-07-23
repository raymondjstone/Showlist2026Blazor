using Bunit;
using System.Linq;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class DedupePageTests : BlazorTestBase
{
    [Fact]
    public async Task NoDuplicates_ShowsSuccessMessage()
    {
        var cut = Render<Dedupe>();

        await cut.Find("button.btn-primary").ClickAsync(new());

        Assert.Contains("No duplicate episode files found", cut.Markup);
    }

    [Fact]
    public async Task DuplicateEpisodeFiles_AreFoundAndCanBeDeleted()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "Showlist2026Tests_Dedupe_" + Guid.NewGuid());
        var showFolder = Path.Combine(basePath, "Breaking Bad");
        var season1 = Path.Combine(showFolder, "Season 1");
        Directory.CreateDirectory(season1);
        File.WriteAllText(Path.Combine(season1, "Breaking.Bad.S01E01.mkv"), "copy one bytes");
        File.WriteAllText(Path.Combine(season1, "Breaking.Bad.S01E01.720p.mkv"), "copy two bytes, longer");

        try
        {
            using (var ctx = Db.CreateContext())
            {
                ctx.TVDirectories.Add(new Showlist2026.Entities.TVDirectories { Name = basePath, DaysToScan = 7 });
                var show = TestData.NewShow("Breaking Bad", folderName: "Breaking Bad", wanted: true);
                ctx.Shows.Add(show);
                ctx.SaveChanges();
            }

            var cut = Render<Dedupe>();
            await cut.Find("button.btn-primary").ClickAsync(new());

            Assert.Contains("files across", cut.Markup);
            Assert.Contains("duplicate groups", cut.Markup);
            Assert.Contains("Breaking.Bad.S01E01", cut.Markup);

            cut.Find("button.btn-outline-danger").Click();

            Assert.Contains("No duplicate episode files found", cut.Markup);
        }
        finally
        {
            Directory.Delete(basePath, recursive: true);
        }
    }

    [Fact]
    public async Task DeletingAFileThatNoLongerExistsOnDisk_ReportsFailure()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "Showlist2026Tests_Dedupe_" + Guid.NewGuid());
        var showFolder = Path.Combine(basePath, "Breaking Bad");
        var season1 = Path.Combine(showFolder, "Season 1");
        Directory.CreateDirectory(season1);
        var duplicateFile = Path.Combine(season1, "Breaking.Bad.S01E01.720p.mkv");
        File.WriteAllBytes(Path.Combine(season1, "Breaking.Bad.S01E01.mkv"), new byte[2000]); // >1KB, exercises the KB size format
        File.WriteAllText(duplicateFile, "copy two bytes, longer");

        try
        {
            using (var ctx = Db.CreateContext())
            {
                ctx.TVDirectories.Add(new Showlist2026.Entities.TVDirectories { Name = basePath, DaysToScan = 7 });
                var show = TestData.NewShow("Breaking Bad", folderName: "Breaking Bad", wanted: true);
                ctx.Shows.Add(show);
                ctx.SaveChanges();
            }

            var cut = Render<Dedupe>();
            await cut.Find("button.btn-primary").ClickAsync(new());
            Assert.Contains("Breaking.Bad.S01E01", cut.Markup);

            // Remove the file out from under the UI between scan and delete, so DeleteFile finds
            // nothing there and the "not found" failure path in the component runs.
            File.Delete(duplicateFile);

            var row = cut.FindAll("tr").First(r => r.TextContent.Contains("720p"));
            row.QuerySelector("button.btn-outline-danger")!.Click();

            Assert.Contains("Could not delete", cut.Markup);
            Assert.Contains("alert-danger", cut.Markup);
        }
        finally
        {
            Directory.Delete(basePath, recursive: true);
        }
    }
}
