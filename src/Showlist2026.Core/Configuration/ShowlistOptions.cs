namespace Showlist2026.Configuration
{
    public class ShowlistOptions
    {
        public string TvNameListPath { get; set; } = @"C:\tvnamelist\";
        public string ShowFolderBasePath { get; set; } = @"F:\tv_name_list\";
        public string TvMazeBaseUrl { get; set; } = "http://api.tvmaze.com";
        public string NzbPlanetApiKey { get; set; } = "";
    }
}
