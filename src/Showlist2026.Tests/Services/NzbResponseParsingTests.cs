using Showlist2026.Entities;
using Showlist2026.Models;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

/// <summary>
/// Tests the internal NZB/Newznab response parsers directly (made `internal` + exposed via
/// [InternalsVisibleTo] specifically for testability - they're pure string-parsing logic with
/// no I/O of their own). The methods that CALL these (CrawlNzbSitesForShow, etc.) construct
/// their own `new HttpClient()` inline rather than using Flurl, so they can't be intercepted by
/// HttpTest; testing the parsing logic directly is the only practical way to cover it.
/// </summary>
public class NzbResponseParsingTests
{
    private static List<Episode> UnwatchedEpisode(long season, long number) =>
        new() { new Episode { season = season, number = number } };

    [Fact]
    public void ParseNewznabResponse_ExtractsMatchingEpisode()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss><channel>
          <item>
            <title>Show.Name.S01E02.720p.mkv</title>
            <link>http://example.com/get/123</link>
            <pubDate>Mon, 01 Jan 2024 00:00:00 +0000</pubDate>
            <newznab:attr xmlns:newznab="http://newznab.com" name="size" value="1500000000" />
          </item>
        </channel></rss>
        """;

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        var summary = new NzbSiteCrawlSummary();

        var results = service.ParseNewznabResponse(xml, "MySite", "http://search?apikey=secret", UnwatchedEpisode(1, 2), summary);

        var result = Assert.Single(results);
        Assert.Equal("S01E02", result.EpisodeCode);
        Assert.Equal("http://example.com/get/123", result.DownloadUrl);
        Assert.Equal("1.50 GB", result.Size); // 1,500,000,000 bytes / 1e9
        Assert.DoesNotContain("secret", result.SearchUrl);
    }

    [Fact]
    public void ParseNewznabResponse_SkipsEpisodesNotInUnwatchedList()
    {
        var xml = """
        <rss><channel>
          <item><title>Show.Name.S01E02.mkv</title><link>http://x/1</link></item>
        </channel></rss>
        """;

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        var summary = new NzbSiteCrawlSummary();

        // Only S02E05 is "unwatched" - S01E02 from the feed doesn't match, so nothing returned.
        var results = service.ParseNewznabResponse(xml, "MySite", "http://search", UnwatchedEpisode(2, 5), summary);

        Assert.Empty(results);
    }

    [Fact]
    public void ParseNewznabResponse_ReturnsEmpty_OnApiErrorElement()
    {
        var xml = """<error code="100" description="Invalid API key" />""";

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        var summary = new NzbSiteCrawlSummary();

        var results = service.ParseNewznabResponse(xml, "MySite", "http://search", UnwatchedEpisode(1, 1), summary);

        Assert.Empty(results);
        Assert.Contains(summary.DebugInfo, d => d.Contains("Invalid API key"));
    }

    [Fact]
    public void ParseNewznabResponse_ReturnsEmpty_OnMalformedXml()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        var summary = new NzbSiteCrawlSummary();

        var results = service.ParseNewznabResponse("not xml at all <<<", "MySite", "http://search", UnwatchedEpisode(1, 1), summary);

        Assert.Empty(results);
        Assert.Contains(summary.DebugInfo, d => d.Contains("XML Parse Error"));
    }

    [Fact]
    public void ParseRssFeedResponse_PrefersEnclosureUrlAndSizeOverLink()
    {
        var xml = """
        <rss><channel>
          <item>
            <title>Show.Name.S01E01.mkv</title>
            <link>http://example.com/page/1</link>
            <enclosure url="http://example.com/download/1.nzb" length="734003200" />
          </item>
        </channel></rss>
        """;

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        var summary = new NzbSiteCrawlSummary();

        var results = service.ParseRssFeedResponse(xml, "MySite", "http://feed?r=secretkey", UnwatchedEpisode(1, 1), summary);

        var result = Assert.Single(results);
        Assert.Equal("http://example.com/download/1.nzb", result.DownloadUrl);
        Assert.Equal("734.0 MB", result.Size); // 734,003,200 bytes / 1e6
        Assert.DoesNotContain("secretkey", result.SearchUrl);
    }

    [Fact]
    public void ParseRssFeedResponse_FallsBackToLink_WhenNoEnclosure()
    {
        var xml = """
        <rss><channel>
          <item>
            <title>Show.Name.S01E01.mkv</title>
            <link>http://example.com/page/1</link>
          </item>
        </channel></rss>
        """;

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        var summary = new NzbSiteCrawlSummary();

        // feedUrl redacts "r=<key>" via `feedUrl.Replace(Regex.Match(feedUrl, "r=[^&]+").Value, ...)`
        // - if the URL has no "r=" param, that regex match is empty and String.Replace("", ...)
        // throws ArgumentException, silently zeroing the results (caught and mislabeled as an
        // "XML Parse Error"). Production always builds these URLs with "&r=<apikey>" already
        // present (see CrawlWithRssFeed), so use a realistic URL here rather than tripping that
        // unrelated fragility.
        var results = service.ParseRssFeedResponse(xml, "MySite", "http://feed?r=apikey123", UnwatchedEpisode(1, 1), summary);

        var result = Assert.Single(results);
        Assert.Equal("http://example.com/page/1", result.DownloadUrl);
    }

    [Fact]
    public void ParseNzbSiteHtml_FindsMatchViaAnchorText()
    {
        var html = """
        <html><body>
          <a href="http://example.com/get/download/1">Show.Name.S01E01.720p</a>
        </body></html>
        """;

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var (results, debugInfo) = service.ParseNzbSiteHtml(html, "MySite", "http://search", UnwatchedEpisode(1, 1));

        var result = Assert.Single(results);
        Assert.Equal("S01E01", result.EpisodeCode);
        Assert.NotEmpty(debugInfo);
    }

    [Fact]
    public void ParseNzbSiteHtml_ReturnsEmpty_WhenNoEpisodeCodeAnywhereInHtml()
    {
        var html = "<html><body><p>Nothing here</p></body></html>";

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var (results, debugInfo) = service.ParseNzbSiteHtml(html, "MySite", "http://search", UnwatchedEpisode(1, 1));

        Assert.Empty(results);
        Assert.NotEmpty(debugInfo); // still logs diagnostic info about what it did/didn't find
    }

    [Fact]
    public void ParseNzbSiteHtml_FindsMatchViaTableRowStrategy_WhenAnchorTextHasNoEpisodeCode()
    {
        var html = """
        <html><body>
          <table>
            <tr><td>Show.Name.S01E01.720p</td><td><a href="http://example.com/download/1">Download</a></td></tr>
          </table>
        </body></html>
        """;

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var (results, _) = service.ParseNzbSiteHtml(html, "MySite", "http://search", UnwatchedEpisode(1, 1));

        var result = Assert.Single(results);
        Assert.Equal("S01E01", result.EpisodeCode);
        Assert.Equal("http://example.com/download/1", result.DownloadUrl);
    }
}
