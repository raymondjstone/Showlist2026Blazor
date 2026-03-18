using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Showlist2026.Entities
{
    [Table("Episode")]
    public class Episode
    {
        [Key] public int Id { get; set; }

        public long episodeid { get; set; }
        public string? name{ get; set; }
        public long? season { get; set; }
        public long? number { get; set; }
        public string? airdate { get; set; }
        public string? airtime { get; set; }
        public string? runtime { get; set; }
        public string? imagemedium { get; set; }
        public string? imageoriginal { get; set; }

        [Column(TypeName = "varchar(MAX)")]
        public string? summary { get; set; }
        public string? links { get; set; }
        //public long unixutc { get; set; }

        public DateTimeOffset? AirDateOffset2 { get; set; }

        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        //public DateTimeOffset? AirdateOffset { get; private set; }

        public Show? show { get; set; }
        public ICollection<UserWatchedSelection>? UserWatchedSelections { get; set; }


        public DateTimeOffset AiringTime
        {
            get
            {
                if (AirDateOffset2 == null)
                {
                    return DateTimeOffset.MinValue;
                }

                DateTimeOffset d = AirDateOffset2??DateTimeOffset.MinValue;

                if (show != null)
                {
                    if (show.Networks != null)
                    {
                        if (show.Networks.tz != null)
                        {
                            // UTC -4 means we add 4 hours to the current 'UTC' time.
                            d = d.AddMinutes(0 - (show.Networks.tz.UTCOffset * 60));
                            return d;
                        }
                    }
                    if (show.WebNetworks != null)
                    {
                        if (show.WebNetworks.tz != null)
                        {
                            d = d.AddMinutes(0 - (show.WebNetworks.tz.UTCOffset * 60));
                            return d;
                        }
                    }
                }



                return d;
            }

        }




        public string nameclean
        {
            get
            {
                return System.Net.WebUtility.HtmlEncode(name);
            }
        }

        public string summarycleanwithimage
        {
            get
            {
                string s = "<div class='row'><div class='col-12'><h2>" + nameclean + "</h2></div></div>";
                s += "<div class='row'><div class='col-4'>";
                s += "<img src='" + imagemedium + "' class='img-fluid' />";
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

        public int runtimeinmins
        {
            get
            {
                if (string.IsNullOrEmpty(runtime))
                {
                    return 60;
                }

                try
                {
                    int x = int.Parse(runtime);
                    if (x < 1)
                    {
                        return 60;
                    }

                    return x;
                }
                catch
                {
                    return 60;
                }
            }
        }

        public string maincountrycode
        {
            get
            {
                if (show != null)
                {
                    if (show.Networks != null)
                    {
                        if (show.Networks.country != null)
                        {
                            return show.Networks.country.code;
                        }
                    }
                    if (show.WebNetworks != null)
                    {
                        if (show.WebNetworks.country != null)
                        {
                            return show.WebNetworks.country.code;
                        }
                    }
                }

                return "";
            }
        }

        public string EpNumberFormatted
        {
            get
            {
                string ep = "S";
                var s = season ?? 0;
                var n = number ?? 0;
                if (s < 10)
                {
                    ep += "0";
                }
                ep += s;

                ep += "E";
                if (n < 10)
                {
                    ep += "0";
                }
                ep += n;

                return ep;
            }

        }
    }
}
