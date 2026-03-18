using System.Text.Json.Serialization;

namespace Showlist2026.TVMaze
{
    public class TVMazeSearchResult
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("show")]
        public TVMazeSearchShow Show { get; set; }
    }

    public class TVMazeSearchShow
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("premiered")]
        public string? Premiered { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("image")]
        public TVMazeSearchImage? Image { get; set; }
    }

    public class TVMazeSearchImage
    {
        [JsonPropertyName("medium")]
        public string? Medium { get; set; }

        [JsonPropertyName("original")]
        public string? Original { get; set; }
    }
}
