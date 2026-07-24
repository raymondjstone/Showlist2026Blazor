using Flurl.Http.Testing;
using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

/// <summary>
/// CrawlNzbSitesForShow/CrawlNzbRssFeedsForShow used to build their own bare `new HttpClient()`
/// instead of going through Flurl, so HttpTest couldn't intercept them at all - these were
/// entirely untestable. Now that they use Flurl's fluent client (see ShowListAppService), these
/// exercise the orchestration around the HTTP call (URL construction, status handling, per-site
/// error aggregation) - the parsing logic itself is covered directly in
/// NzbResponseParsingTests.cs.
/// </summary>
public class ShowListAppServiceCrawlTests
{
    private static Show MakeShowWithUnwatchedEpisode(string name = "Breaking Bad")
    {
        var show = TestData.NewShow(name, wanted: true, showid: 42);
        TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));
        return show;
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_ReportsError_WhenShowDoesNotExist()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var summary = await service.CrawlNzbSitesForShow(999);

        Assert.Contains(summary.Errors, e => e == "Show not found");
        Assert.Equal(0, summary.SitesCrawled);
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_ReportsError_WhenNoActiveSitesConfigured()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            // Only an inactive site configured.
            ctx.TVSites.Add(new TVSite { Name = "Inactive", URLTemplate = "http://example.com/search?q={URLSearchTerm}", Active = false, Order = 1 });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Contains(summary.Errors, e => e == "No active search sites configured");
        Assert.Equal(0, summary.SitesCrawled);
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_HtmlScraping_FindsAndReportsResults()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("""
            <html><body>
              <a href="http://example.com/download/1">Breaking.Bad.S01E01.720p</a>
            </body></html>
            """);

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite { Name = "MySite", URLTemplate = "http://example.com/search?q={URLSearchTerm}", Active = true, Order = 1 });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(1, summary.SitesCrawled);
        Assert.Single(summary.Results);
        Assert.Equal("http://example.com/download/1", summary.Results[0].DownloadUrl);
        httpTest.ShouldHaveCalled("http://example.com/search*");
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_HtmlScraping_ReportsHttpFailure()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("not found", 404);

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite { Name = "MySite", URLTemplate = "http://example.com/search?q={URLSearchTerm}", Active = true, Order = 1 });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        Assert.Empty(summary.Results);
        Assert.Contains(summary.Errors, e => e.Contains("HTTP 404"));
        var crawled = Assert.Single(summary.CrawledUrls);
        Assert.False(crawled.Success);
        Assert.Equal(404, crawled.HttpStatus);
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_UsesNewznabApi_WhenApiKeyConfigured()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("""
            <?xml version="1.0" encoding="UTF-8"?>
            <rss><channel>
              <item>
                <title>Breaking.Bad.S01E01.720p</title>
                <link>http://example.com/get/1</link>
                <pubDate>Mon, 01 Jan 2024 00:00:00 +0000</pubDate>
                <newznab:attr xmlns:newznab="http://newznab.com" name="size" value="1500000000" />
              </item>
            </channel></rss>
            """);

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyApiSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                ApiKey = "secretkey",
                ApiBaseUrl = "http://api.example.com",
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(1, summary.SitesCrawled);
        Assert.Single(summary.Results);
        httpTest.ShouldHaveCalled("http://api.example.com/api*apikey=secretkey*");
        // The API key must never be exposed in the recorded crawl URL.
        var crawled = Assert.Single(summary.CrawledUrls);
        Assert.DoesNotContain("secretkey", crawled.Url);
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_DerivesApiBaseUrl_FromUrlTemplate_WhenApiBaseUrlNotSet()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("""
            <?xml version="1.0" encoding="UTF-8"?>
            <rss><channel>
              <item>
                <title>Breaking.Bad.S01E01.720p</title>
                <link>http://example.com/get/1</link>
                <pubDate>Mon, 01 Jan 2024 00:00:00 +0000</pubDate>
              </item>
            </channel></rss>
            """);

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyApiSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                ApiKey = "secretkey",
                ApiBaseUrl = null, // must derive "http://example.com" from URLTemplate
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(1, summary.SitesCrawled);
        httpTest.ShouldHaveCalled("http://example.com/api*apikey=secretkey*");
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_DerivesNzbGeekApiBaseUrl_ForNzbGeekUrlTemplate()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("<?xml version=\"1.0\"?><rss><channel></channel></rss>");

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "NzbGeek",
                URLTemplate = "https://nzbgeek.info/geekseek.php?q={URLSearchTerm}",
                ApiKey = "secretkey",
                ApiBaseUrl = null,
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.CrawlNzbSitesForShow(showId);

        httpTest.ShouldHaveCalled("https://api.nzbgeek.info/api*");
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_SkipsApiCrawl_WhenNoApiBaseUrlCanBeDetermined()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "NoUrlTemplate",
                URLTemplate = null, // nothing to derive an API base from
                ApiKey = "secretkey",
                ApiBaseUrl = null,
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        Assert.Empty(summary.Results);
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_ReportsApiHttpFailure()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("not found", 404);

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyApiSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                ApiKey = "secretkey",
                ApiBaseUrl = "http://api.example.com",
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        Assert.Empty(summary.Results);
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_ReportsApiException()
    {
        using var httpTest = new HttpTest();
        httpTest.SimulateException(new InvalidOperationException("boom"));

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyApiSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                ApiKey = "secretkey",
                ApiBaseUrl = "http://api.example.com",
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        Assert.Empty(summary.Results);
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_HtmlScraping_ReportsTimeout()
    {
        using var httpTest = new HttpTest();
        httpTest.SimulateTimeout();

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite { Name = "MySite", URLTemplate = "http://example.com/search?q={URLSearchTerm}", Active = true, Order = 1 });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        Assert.Contains(summary.Errors, e => e.Contains("Timeout"));
        var crawled = Assert.Single(summary.CrawledUrls);
        Assert.Equal("Timeout", crawled.ErrorMessage);
    }

    [Fact]
    public async Task CrawlNzbSitesForShow_HtmlScraping_ReportsUnexpectedException()
    {
        using var httpTest = new HttpTest();
        httpTest.SimulateException(new InvalidOperationException("network stack exploded"));

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite { Name = "MySite", URLTemplate = "http://example.com/search?q={URLSearchTerm}", Active = true, Order = 1 });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbSitesForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        Assert.Contains(summary.Errors, e => e.Contains("network stack exploded"));
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_FindsAndReportsResults()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("""
            <rss><channel>
              <item>
                <title>Breaking.Bad.S01E01.720p</title>
                <link>http://example.com/page/1</link>
                <enclosure url="http://example.com/download/1.nzb" length="734003200" />
              </item>
            </channel></rss>
            """);

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyRssSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                RssApiKey = "rsskey",
                RssBaseUrl = "http://rss.example.com",
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbRssFeedsForShow(showId);

        Assert.Equal(1, summary.SitesCrawled);
        Assert.Single(summary.Results);
        Assert.Equal("http://example.com/download/1.nzb", summary.Results[0].DownloadUrl);
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_ReportsHttpFailure()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("error", 500);

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyRssSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                RssApiKey = "rsskey",
                RssBaseUrl = "http://rss.example.com",
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbRssFeedsForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        var crawled = Assert.Single(summary.CrawledUrls);
        Assert.False(crawled.Success);
        Assert.Equal(500, crawled.HttpStatus);
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_ReportsError_WhenShowDoesNotExist()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var summary = await service.CrawlNzbRssFeedsForShow(999);

        Assert.Contains(summary.Errors, e => e == "Show not found");
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_ReportsError_WhenNoSiteHasRssApiKeyConfigured()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite { Name = "NoRssKey", URLTemplate = "http://example.com/search?q={URLSearchTerm}", Active = true, Order = 1 });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbRssFeedsForShow(showId);

        Assert.Contains(summary.Errors, e => e == "No sites configured with RSS API keys");
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_DerivesRssBaseUrl_FromApiBaseUrl_WhenRssBaseUrlNotSet()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("<rss><channel></channel></rss>");

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyRssSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                RssApiKey = "rsskey",
                RssBaseUrl = null,
                ApiBaseUrl = "http://api.example.com",
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.CrawlNzbRssFeedsForShow(showId);

        httpTest.ShouldHaveCalled("http://api.example.com/api*");
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_DerivesRssBaseUrl_FromUrlTemplate_WhenNeitherRssNorApiBaseUrlSet()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("<rss><channel></channel></rss>");

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyRssSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                RssApiKey = "rsskey",
                RssBaseUrl = null,
                ApiBaseUrl = null,
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.CrawlNzbRssFeedsForShow(showId);

        httpTest.ShouldHaveCalled("http://example.com/api*");
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_SkipsCrawl_WhenNoRssBaseUrlCanBeDetermined()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "NoUrlTemplate",
                URLTemplate = null,
                RssApiKey = "rsskey",
                RssBaseUrl = null,
                ApiBaseUrl = null,
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbRssFeedsForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        Assert.Empty(summary.Results);
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_ReportsTimeout()
    {
        using var httpTest = new HttpTest();
        httpTest.SimulateTimeout();

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyRssSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                RssApiKey = "rsskey",
                RssBaseUrl = "http://rss.example.com",
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbRssFeedsForShow(showId);

        // CrawlWithRssFeed catches the timeout internally (never rethrows), so it's reported via
        // DebugInfo/CrawledUrls rather than the outer per-site catch's summary.Errors.
        Assert.Equal(0, summary.SitesCrawled);
        var crawled = Assert.Single(summary.CrawledUrls);
        Assert.False(crawled.Success);
        Assert.Contains(summary.DebugInfo, d => d.Contains("RSS Error"));
    }

    [Fact]
    public async Task CrawlNzbRssFeedsForShow_ReportsUnexpectedException()
    {
        using var httpTest = new HttpTest();
        httpTest.SimulateException(new InvalidOperationException("network stack exploded"));

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = MakeShowWithUnwatchedEpisode();
            ctx.Shows.Add(show);
            ctx.TVSites.Add(new TVSite
            {
                Name = "MyRssSite",
                URLTemplate = "http://example.com/search?q={URLSearchTerm}",
                RssApiKey = "rsskey",
                RssBaseUrl = "http://rss.example.com",
                Active = true,
                Order = 1
            });
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var summary = await service.CrawlNzbRssFeedsForShow(showId);

        Assert.Equal(0, summary.SitesCrawled);
        Assert.Contains(summary.DebugInfo, d => d.Contains("network stack exploded"));
    }
}
