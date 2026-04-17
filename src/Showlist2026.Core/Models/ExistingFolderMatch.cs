namespace Showlist2026.Models;

public class ExistingFolderMatch
{
    public string FolderName { get; set; } = "";
    public string FullPath { get; set; } = "";
    public DateTime FolderDate { get; set; }
    public string? EarliestEpisode { get; set; }
    public string? LatestEpisode { get; set; }
}
