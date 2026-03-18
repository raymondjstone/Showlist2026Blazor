using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Showlist2026.TVMaze
{
    namespace TVMazeEpisodes
    {

        public partial class EpisodeData
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("season")]
            public long Season { get; set; }

            [JsonPropertyName("number")]
            public long? Number { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("airdate")]
            public string? Airdate { get; set; }

            [JsonPropertyName("airtime")]
            public string? Airtime { get; set; }

            [JsonPropertyName("airstamp")]
            public string? Airstamp { get; set; }

            [JsonPropertyName("runtime")]
            public long? Runtime { get; set; }

            [JsonPropertyName("image")]
            public Image? Image { get; set; }

            [JsonPropertyName("summary")]
            public string? Summary { get; set; }

            [JsonPropertyName("_links")]
            public Links? Links { get; set; }
        }

        public partial class Image
        {
            [JsonPropertyName("medium")]
            public string? Medium { get; set; }

            [JsonPropertyName("original")]
            public string? Original { get; set; }
        }

        public partial class Links
        {
            [JsonPropertyName("self")]
            public Self? Self { get; set; }
        }

        public partial class Self
        {
            [JsonPropertyName("href")]
            public string? Href { get; set; }
        }

    }

}
