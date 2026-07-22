using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using STJ = System.Text.Json.Serialization;

// Originally auto-generated (quicktype) from a sample NZBPlanet API response.
//
// NOTE: production (ShowListAppService.NZBPlanetSearch) deserializes this via Flurl's
// GetJsonAsync<T>(), which uses System.Text.Json by default - Flurl.Http.Newtonsoft is not
// referenced anywhere in this project. System.Text.Json ignores Newtonsoft's [JsonProperty]
// entirely, and would never match "@attributes" (not a valid property name in any naming
// convention) even with case-insensitive matching. That silently left every
// Item.Attr[i].Attributes null in production, so Item.Size/Season/Episode/Sortkey were always
// empty/default regardless of what the API returned. The [STJ.JsonPropertyName] attributes
// below fix that for the System.Text.Json path; the Newtonsoft [JsonProperty] attributes are
// kept alongside them for documentation of the real API's field names.
//
// The original generated file also included a Newtonsoft-only deserialization path
// (NzBplanet.FromJson / Serialize.ToJson / Converter / NameConverter / ParseStringConverter)
// and a Response/ResponseAttributes type for a "response" field - none of these were ever
// wired up or referenced by the app (Channel.Response was commented out from the start), so
// they were removed rather than fixed.

namespace Showlist2026.NZBPlanetApiJSON
{
    public partial class NzBplanetJSON
    {
        [JsonProperty("@attributes"), STJ.JsonPropertyName("@attributes")]
        public NzBplanetAttributes Attributes { get; set; }

        [JsonProperty("channel"), STJ.JsonPropertyName("channel")]
        public Channel Channel { get; set; }

    }

    public partial class NzBplanetAttributes
    {
        [JsonProperty("version"), STJ.JsonPropertyName("version")]
        public string Version { get; set; }
    }

    public partial class Channel
    {
        [JsonProperty("title"), STJ.JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("description"), STJ.JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonProperty("link"), STJ.JsonPropertyName("link")]
        public Uri Link { get; set; }

        [JsonProperty("language"), STJ.JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonProperty("item"), STJ.JsonPropertyName("item")]
        public List<Item> Item { get; set; }
    }

    public partial class Image
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("link")]
        public Uri Link { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public partial class Item
    {
        [JsonProperty("title"), STJ.JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty("guid"), STJ.JsonPropertyName("guid")]
        public Uri Guid { get; set; }

        [JsonProperty("link"), STJ.JsonPropertyName("link")]
        public Uri Link { get; set; }

        [JsonProperty("comments"), STJ.JsonPropertyName("comments")]
        public Uri Comments { get; set; }

        [JsonProperty("pubDate"), STJ.JsonPropertyName("pubDate")]
        public string PubDate { get; set; }

        [JsonProperty("category"), STJ.JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonProperty("description"), STJ.JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonProperty("enclosure"), STJ.JsonPropertyName("enclosure")]
        public Enclosure Enclosure { get; set; }

        [JsonProperty("attr"), STJ.JsonPropertyName("attr")]
        public List<Attr> Attr { get; set; }


        // Create a key that can be used to order shows by manual preference
        public int Sortkey
        {
            get
            {
                int s = 0;
                if (Category.ToLower().Contains("foreign"))
                {
                    s = 500;
                }
                if (Category.EndsWith("SD"))
                {
                    if (Title.ToLower().Contains("x264") || Title.ToLower().Contains("x265"))
                    {
                        return s+10;
                    }
                    return s + 20;
                }

                if(Title.ToLower().Contains("x265"))
                {
                    return s + 30;
                }
                if (SizeAsNumberMBs < 501)
                {
                    return s + 40;
                }

                return SizeAsNumberMBs + s + 10000;
            }
        }

        public int SizeAsNumber
        {
            get
            {
                int x = 0;
                bool ok = int.TryParse(Size, out x);
                if (ok)
                {
                    return x;
                }
                return int.MaxValue;
            }
        }
        public int SizeAsNumberMBs
        {
            get
            {
                return (int)Math.Round((decimal)SizeAsNumber / 1024 / 1024, 0);
            }
        }


        // Some methods to get the attributes without needing to code it all over the place
        public string Size
        {
            get
            {
                return GetAttr(Name.Size);
            }
        }
        public string Season
        {
            get
            {
                return GetAttr(Name.Season)??"";
            }
        }
        public string Episode
        {
            get
            {
                return GetAttr(Name.Episode)??"";
            }
        }
        public string GetAttr(Name name)
        {
             return Attr?.FirstOrDefault(a => a.Attributes?.Name == name)?.Attributes?.Value;
        }

        public string EpNumberFormatted
        {
            get
            {
                return $"{Season}{Episode}";
            }
        }

    }

    public partial class Attr
    {
        [JsonProperty("@attributes"), STJ.JsonPropertyName("@attributes")]
        public AttrAttributes Attributes { get; set; }
    }

    public partial class AttrAttributes
    {
        // System.Text.Json has no built-in string<->enum mapping without an explicit converter
        // (it expects the numeric value by default). JsonStringEnumConverter reads member names
        // case-insensitively, which matches the lowercase JSON values ("season", "episode", ...)
        // against these enum members (Season, Episode, ...) with no further mapping needed.
        [JsonProperty("name"), STJ.JsonPropertyName("name"), STJ.JsonConverter(typeof(STJ.JsonStringEnumConverter))]
        public Name Name { get; set; }

        [JsonProperty("value"), STJ.JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public partial class Enclosure
    {
        [JsonProperty("@attributes"), STJ.JsonPropertyName("@attributes")]
        public EnclosureAttributes Attributes { get; set; }
    }

    public partial class EnclosureAttributes
    {
        [JsonProperty("url"), STJ.JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonProperty("length"), STJ.JsonPropertyName("length")]
        public string Length { get; set; }

        [JsonProperty("type"), STJ.JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public enum Name { Category, Comments, Episode, Grabs, Imdb, Password, Season, Size, Tvairdate, Tvtitle, Usenetdate };
}
