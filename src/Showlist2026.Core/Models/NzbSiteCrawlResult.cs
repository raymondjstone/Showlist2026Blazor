namespace Showlist2026.Models;

public class NzbSiteCrawlResult
{
    public string SiteName { get; set; } = "";
    public string EpisodeCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Size { get; set; } = "";
    public DateTime? PostDate { get; set; }
    public string SearchUrl { get; set; } = "";
}

public class NzbSiteCrawlSummary
{
    public List<NzbSiteCrawlResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<CrawledSiteInfo> CrawledUrls { get; set; } = new();
    public int SitesCrawled { get; set; }
    public int TotalResults { get; set; }
    public List<string> DebugInfo { get; set; } = new();
}

public class CrawledSiteInfo
{
    public string SiteName { get; set; } = "";
    public string Url { get; set; } = "";
    public int HttpStatus { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
