using System;
using System.Collections.Generic;

namespace Showlist2026.Models
{
    public class ExportModel
    {
        public DateTime ExportDate { get; set; } = DateTime.UtcNow;
        public List<ExportShowSelection> ShowSelections { get; set; } = new();
        public List<ExportWatchedEpisode> WatchedEpisodes { get; set; } = new();
    }

    public class ExportShowSelection
    {
        public long TvMazeShowId { get; set; }
        public string ShowName { get; set; } = "";
        public bool Include { get; set; }
        public string? FolderName { get; set; }
    }

    public class ExportWatchedEpisode
    {
        public long TvMazeShowId { get; set; }
        public string ShowName { get; set; } = "";
        public long? Season { get; set; }
        public long? EpisodeNumber { get; set; }
    }

    public class ImportPathsPreview
    {
        public List<ImportPathsShowMatch> MatchedShows { get; set; } = new();
        public List<string> UnmatchedFolders { get; set; } = new();
        public int LinesSkipped { get; set; }
        public int TotalEpisodes { get; set; }
    }

    public class ImportPathsShowMatch
    {
        public int ShowId { get; set; }
        public string FolderName { get; set; } = "";
        public string ShowName { get; set; } = "";
        public int EpisodeCount { get; set; }
        public string EpisodeRange { get; set; } = "";
        public bool AlreadyWanted { get; set; }
    }
}
