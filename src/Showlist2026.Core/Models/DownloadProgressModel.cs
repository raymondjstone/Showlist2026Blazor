using System.Collections.Generic;
using Showlist2026.Entities;

namespace Showlist2026.Models
{
    public class DownloadProgressModel
    {
        public string ShowName { get; set; } = "";
        public int ShowId { get; set; }
        public int TotalAiredEpisodes { get; set; }
        public int DownloadedEpisodes { get; set; }
        public int MissingCount => TotalAiredEpisodes - DownloadedEpisodes;
        public int PercentComplete => TotalAiredEpisodes > 0 ? (DownloadedEpisodes * 100) / TotalAiredEpisodes : 0;
        public List<Episode> MissingEpisodes { get; set; } = new();
    }
}
