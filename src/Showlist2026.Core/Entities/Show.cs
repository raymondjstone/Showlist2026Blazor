using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;

namespace Showlist2026.Entities
{
    [Table("Show")]
    public class Show
    {
        [Key] public int Id { get; set; }

        public long showid { get; set; }
        public long page { get; set; }
        public string? name{ get; set; }
        public string? url { get; set; }
        public string? status { get; set; }
        public string? scheduletime { get; set; }
        public string? scheduledays { get; set; }
        public string? premiered { get; set; }
        public string? summary { get; set; }
        public string? updated { get; set; }
        public string? imagemed { get; set; }
        public string? imageorig { get; set; }
        public bool needsupdate { get; set; }



        public List<Episode>? Episodes { get; set; }
        public Network? Networks { get; set; }
        public WebNetwork? WebNetworks { get; set; }
        public Type? Types { get; set; }
        public Language? Languages { get; set; }


        public ICollection<Genre>? Genres { get; set; }
        public ICollection<ShowFolderAlias>? FolderAliases { get; set; }

        /// <summary>
        /// null = undecided, true = wanted, false = explicitly excluded
        /// </summary>
        public bool? Wanted { get; set; }
        public int Priority { get; set; }

        public string? FolderName { get; set; }
        public string? Notes { get; set; }

        [MaxLength(30)] public string? tvrage     { get; set; }
        [MaxLength(30)] public string? thetvdb    { get; set; }
        [MaxLength(30)] public string? imdb       { get; set; }


        public string URLSearchTerm
        {
            get
            {
                string s = DefaultFolderName;
                if (!string.IsNullOrEmpty(FolderName))
                {
                    s = FolderName;
                }
                s = s.Replace(".", " ");
                return s+ "?t=5000";
            }
        }
        public string URLSearchTermNameOnly
        {
            get
            {
                string s = DefaultFolderName;
                if (!string.IsNullOrEmpty(FolderName))
                {
                    s = FolderName;
                }
                s = s.Replace(".", " ");
                return s;
            }
        }
        public string URLSearchTermGeekSeek
        {
            get
            {
                string s = DefaultFolderName;
                if (!string.IsNullOrEmpty(FolderName))
                {
                    s = FolderName;
                }
                s = s.Replace(".", " ");
                return s;
            }
        }

        public string nameclean
        {
            get
            {
                return System.Net.WebUtility.HtmlEncode(name ?? "");
            }
        }

        public string select2ResultName
        {
            get
            {
                if (ShowStart > DateTime.MinValue)
                {
                    return $"{nameclean} ({ShowStart.Year})";
                }
                return $"{nameclean}";
            }
        }



        public string summarycleanwithimage
        {
            get
            {
                string s = "<div class='row'><div class='col-12'><h2>"+nameclean+"</h2></div></div>";
                s += "<div class='row'><div class='col-4'>";
                s += "<img src='" + imagemed + "' class='img-fluid' />";
                s += "</div><div class='col-8'>";
                s += summaryclean;
                s += "</div></div>";
                return s;
            }
        }


        public string summaryclean
        {
            get
            {
                if (!String.IsNullOrEmpty(summary))
                {
                    return summary.Replace("\"", "'");
                }
                return "";
            }
        }


        public DateTime ShowStart
        {
            get
            {
                if (string.IsNullOrEmpty(premiered))
                {
                    return DateTime.MinValue;
                }

                string[] s = premiered.Split(' ');
                if (DateTime.TryParse(s[0], new CultureInfo("en-US"), DateTimeStyles.None, out var d))
                {
                    return d;
                }
                if (DateTime.TryParse(s[0], new CultureInfo("en-GB"), DateTimeStyles.None, out d))
                {
                    return d;
                }
                return DateTime.MinValue;
            }
        }
        public string DefaultFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return "";
                }
                return regexp.Replace("^", "").Replace("..", ".").Replace("..", ".").Replace(".", " ").Trim();
            }
        }

        /// <summary>
        /// Folder name suggestion that appends the air year when another show shares the same name.
        /// Set by the service layer; falls back to DefaultFolderName if not populated.
        /// </summary>
        [NotMapped]
        public string SuggestedFolderName { get; set; }

        public string  regexp
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return "";
                }
                return "^" + name.Replace(@"&", ".and.").Replace(@"'", "").Replace(@"\u2019", "").Replace(@":", "").Replace(" ", ".").Replace(",", ".")
                    .Replace(@"#", ".").Replace(@"+", ".and.").Replace(@"!", ".")
                    .Replace(@"/", ".").Replace(@"\", ".").Replace(@"?", ".").Replace(@"-", ".").Replace("..",".").Trim();
            }
        }

        public string namecleanFolderFriendly
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return "";
                }
                return "^" + nameclean.Replace("'", "").Replace(":", "");
            }
        }

        public string showPageURL
        {
            get
            {
                return $"/showlist/show/{Id}";
            }
        }


        public Episode? nextEpisode
        {
            get
            {
                if (Episodes == null || Episodes.Count < 1)
                {
                    return null;
                }

                var t = Episodes.Where(a => a.AiringTime >= DateTimeOffset.Now)
                    .OrderBy(a => a.AiringTime)
                    .FirstOrDefault();

                if (t != null)
                {
                    return t;
                }

                //If no next ep show last one
                return Episodes
                    .OrderByDescending(a => a.AiringTime)
                    .FirstOrDefault();
            }
        }

    }
}
