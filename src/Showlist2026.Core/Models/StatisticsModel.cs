using System.Collections.Generic;

namespace Showlist2026.Models
{
    public class StatisticsModel
    {
        public int TotalShowsTracked { get; set; }
        public int ActiveShows { get; set; }
        public int CompletedShows { get; set; }
        public int TotalEpisodesWatched { get; set; }
        public int TotalWatchTimeMinutes { get; set; }
        public Dictionary<string, int> EpisodesWatchedPerMonth { get; set; } = new();
        public Dictionary<string, int> GenreBreakdown { get; set; } = new();
        public List<ShowWatchStat> MostWatchedShows { get; set; } = new();
    }

    public class ShowWatchStat
    {
        public string ShowName { get; set; } = "";
        public int ShowId { get; set; }
        public int EpisodesWatched { get; set; }
        public int TotalEpisodes { get; set; }
    }
}
