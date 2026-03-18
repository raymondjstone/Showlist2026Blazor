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

        // <a href="https://nzbs.in/search/@Model.show.URLSearchTerm" target="_blank">NZbs.In</a>
        // <a href="https://nzbplanet.net/search/@Model.show.URLSearchTerm" target="_blank">nzbplanet.net</a>
        // <a href="https://nzbgeek.info/geekseek.php?moviesgeekseek=1&c=5000&browseincludewords=@Model.show.URLSearchTermGeekSeek" target="_blank">geekseek</a>




    }
}
