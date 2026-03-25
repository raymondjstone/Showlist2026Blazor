namespace Showlist2026.Models;

public class DuplicateFileEntry
{
    public string ShowName { get; set; } = "";
    public string ShowFolderName { get; set; } = "";
    public long Season { get; set; }
    public long Episode { get; set; }
    public string Directory { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string GroupKey => $"{ShowFolderName.ToLower()}|S{Season:D2}E{Episode:D2}";
}
