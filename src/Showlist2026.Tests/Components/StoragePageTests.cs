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
}
