using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("TVSites")]
    public class TVSite
    {
        [Key] public int Id { get; set; }

        public string? Name{ get; set; }
        public string?  URLTemplate { get; set; }
        public int Order { get; set; }
        public bool Active { get; set; }

        /// <summary>
        /// API key for Newznab-compatible API access (optional).
        /// If set, the crawler will use the API endpoint instead of HTML scraping.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Base URL for API calls (e.g., "https://api.nzbgeek.info").
        /// If not set, will try to derive from URLTemplate.
        /// </summary>
        public string? ApiBaseUrl { get; set; }

        /// <summary>
        /// RSS API key for RSS feed access (optional).
        /// If set, enables RSS feed crawling for this site.
        /// Sites without this key will be skipped during RSS crawls.
        /// </summary>
        public string? RssApiKey { get; set; }

        /// <summary>
        /// Base URL for RSS feeds (e.g., "https://api.nzbgeek.info").
        /// If not set, will try to derive from URLTemplate or ApiBaseUrl.
        /// </summary>
        public string? RssBaseUrl { get; set; }
        // <a href="https://nzbplanet.net/search/@Model.show.URLSearchTerm" target="_blank">nzbplanet.net</a>
        // <a href="https://nzbgeek.info/geekseek.php?moviesgeekseek=1&c=5000&browseincludewords=@Model.show.URLSearchTermGeekSeek" target="_blank">geekseek</a>
    }
}
