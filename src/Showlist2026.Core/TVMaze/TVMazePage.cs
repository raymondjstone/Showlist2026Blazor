using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Showlist2026.TVMaze
{
    namespace TVMazePage
    {

        public partial class TVMazeShowData
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("language")]
            public string? Language { get; set; }

            [JsonPropertyName("genres")]
            public List<string>? Genres { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("runtime")]
            public long? Runtime { get; set; }

            [JsonPropertyName("premiered")]
            public string? Premiered { get; set; }

            [JsonPropertyName("officialSite")]
            public string? OfficialSite { get; set; }

            [JsonPropertyName("schedule")]
            public Schedule? Schedule { get; set; }

            [JsonPropertyName("rating")]
            public Rating? Rating { get; set; }

            [JsonPropertyName("weight")]
            public long Weight { get; set; }

            [JsonPropertyName("network")]
            public Network? Network { get; set; }

            [JsonPropertyName("webChannel")]
            public Network? WebChannel { get; set; }

            [JsonPropertyName("externals")]
            public Externals? Externals { get; set; }

            [JsonPropertyName("image")]
            public Image? Image { get; set; }

            [JsonPropertyName("summary")]
            public string? Summary { get; set; }

            [JsonPropertyName("updated")]
            public long Updated { get; set; }

            [JsonPropertyName("_links")]
            public Links? Links { get; set; }
        }

        public partial class Externals
        {
            [JsonPropertyName("tvrage")]
            public long? Tvrage { get; set; }

            [JsonPropertyName("thetvdb")]
            public long? Thetvdb { get; set; }

            [JsonPropertyName("imdb")]
            public string? Imdb { get; set; }
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
            public Nextepisode? Self { get; set; }

            [JsonPropertyName("previousepisode")]
            public Nextepisode? Previousepisode { get; set; }

            [JsonPropertyName("nextepisode")]
            public Nextepisode? Nextepisode { get; set; }
        }

        public partial class Nextepisode
        {
            [JsonPropertyName("href")]
            public string? Href { get; set; }
        }

        public partial class Network
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("country")]
            public Country? Country { get; set; }
        }

        public partial class Country
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("code")]
            public string? Code { get; set; }

            [JsonPropertyName("timezone")]
            public string? Timezone { get; set; }
        }

        public partial class Rating
        {
            [JsonPropertyName("average")]
            public double? Average { get; set; }
        }

        public partial class Schedule
        {
            [JsonPropertyName("time")]
            public string? Time { get; set; }

            [JsonPropertyName("days")]
            public List<string>? Days { get; set; }
        }

    }

}
