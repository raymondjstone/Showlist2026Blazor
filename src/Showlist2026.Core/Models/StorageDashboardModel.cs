namespace Showlist2026.Models
{
    public class StorageDashboardModel
    {
        public long TotalSizeBytes { get; set; }
        public int TotalFolders { get; set; }
        public int MatchedFolders { get; set; }
        public int UnmatchedFolders { get; set; }
        public List<ShowStorageInfo> Shows { get; set; } = new();
        public List<string> UnmatchedFolderNames { get; set; } = new();
    }

    public class ShowStorageInfo
    {
        public int ShowId { get; set; }
        public string ShowName { get; set; } = "";
        public string FolderName { get; set; } = "";
        public long SizeBytes { get; set; }
        public int FileCount { get; set; }
        public int SeasonCount { get; set; }
        public string Status { get; set; } = "";
        public bool IsWanted { get; set; }

        public string SizeFormatted
        {
            get
            {
                if (SizeBytes >= 1_099_511_627_776) return $"{SizeBytes / 1_099_511_627_776.0:F1} TB";
                if (SizeBytes >= 1_073_741_824) return $"{SizeBytes / 1_073_741_824.0:F1} GB";
                if (SizeBytes >= 1_048_576) return $"{SizeBytes / 1_048_576.0:F1} MB";
                return $"{SizeBytes / 1024.0:F1} KB";
            }
        }
    }
}
