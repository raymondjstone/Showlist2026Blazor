using Showlist2026.Entities;

namespace Showlist2026.Models
{
    public class ShowComparisonModel
    {
        public ShowComparisonSide Show1 { get; set; } = new();
        public ShowComparisonSide Show2 { get; set; } = new();
    }

    public class ShowComparisonSide
    {
        public Show? Show { get; set; }
        public int TotalEpisodes { get; set; }
        public int WatchedEpisodes { get; set; }
        public int AiredEpisodes { get; set; }
        public string Genres { get; set; } = "";
        public string? Network { get; set; }
        public int Seasons { get; set; }
    }
}
