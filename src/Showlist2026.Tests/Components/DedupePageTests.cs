using Bunit;
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
}
