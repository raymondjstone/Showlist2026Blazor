using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Showlist2026.Models
{
    public class AiringAroundNowTabModel
    {
        public List<EpFilter> _EpisodeList;
        public string _Name;
        public string _TabId;

        public AiringAroundNowTabModel(string Name, List<EpFilter> EpisodeList)
        {
            _Name = Name;
            _TabId = "T" + Regex.Replace(_Name, @"[^a-zA-Z0-9]", "x");
            _EpisodeList = EpisodeList ?? new List<EpFilter>();
        }
    }
}
