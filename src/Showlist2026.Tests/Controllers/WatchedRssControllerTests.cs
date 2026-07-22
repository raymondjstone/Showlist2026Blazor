using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Controllers;
using Xunit;

namespace Showlist2026.Tests.Controllers;

public class WatchedRssControllerTests
{
    private static WatchedRssController MakeController(Showlist2026.Data.ShowlistDbContext db)
    {
        var controller = new WatchedRssController(db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request = { Scheme = "https", Host = new HostString("showlist.example") }
            }
        };
        return controller;
    }

    [Fact]
    public async Task Get_ReturnsRssFeed_ContainingOnlyWatchedEpisodesWithAirDate()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("My Show");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1), watched: true, episodeid: 111);
            TestData.NewEpisode(show, 1, 2, watched: false); // not watched -> excluded
            TestData.NewEpisode(show, 1, 3, watched: true); // watched but no air date -> excluded
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        using var context = db.CreateContext();
        var controller = MakeController(context);

        var result = Assert.IsType<FileStreamResult>(await controller.Get());
        Assert.Equal("application/rss+xml", result.ContentType);

        using var reader = new StreamReader(result.FileStream);
        var xml = await reader.ReadToEndAsync();

        Assert.Contains("My Show", xml);
        Assert.Contains("S01E01", xml);
        Assert.Contains("rss/watched/torrent/111/", xml);
        Assert.DoesNotContain("S01E02", xml);
        Assert.DoesNotContain("S01E03", xml);
    }

    [Fact]
    public void Torrent_ReturnsValidBencodedFileWithGivenName()
    {
        using var db = new TestDb();
        using var context = db.CreateContext();
        var controller = MakeController(context);

        var result = Assert.IsType<FileContentResult>(controller.Torrent(1, "My.Show.S01E01.torrent"));

        Assert.Equal("application/x-bittorrent", result.ContentType);
        Assert.Equal("My.Show.S01E01.torrent", result.FileDownloadName);

        var bencoded = System.Text.Encoding.UTF8.GetString(result.FileContents);
        Assert.Contains("4:name14:My.Show.S01E01", bencoded); // "My.Show.S01E01" is 14 chars
    }
}
