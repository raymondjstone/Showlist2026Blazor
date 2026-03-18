namespace Showlist2026.Models
{
    public class TrendingShowModel
    {
        public long TvMazeId { get; set; }
        public string Name { get; set; } = "";
        public string? Network { get; set; }
        public string? ImageUrl { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public string? Summary { get; set; }
        public int EpisodeCount { get; set; }
        public bool AlreadyTracked { get; set; }
        public int? LocalShowId { get; set; }
    }
}
