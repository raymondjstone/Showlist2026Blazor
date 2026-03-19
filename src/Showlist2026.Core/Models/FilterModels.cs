using System;
using System.Collections.Generic;
using Showlist2026.Entities;

namespace Showlist2026.Models
{
    public class ShowFilter
    {
        public Show ep;
        public bool privuser;
        public bool activelyselected;
        public bool activelyignored;
        public bool activelywatched;
        public bool? showinclude;
        public bool? networkinclude;
        public bool? webnetworkinclude;
        public bool? typeinclude;
        public bool? countryinclude;
        public bool? languageinclude;
        public bool? genreinclude;
        public ShowFilter()
        {
            ep = null;
            activelyignored = false;
            activelyselected = false;
            activelywatched = false;
            privuser = false;
        }
        public ShowFilter(Show e)
        {
            ep = e;
            activelyignored = false;
            activelyselected = false;
            activelywatched = false;
            privuser = false;
        }
        public ShowFilter(EpFilter efilter)
        {
            ep = efilter.ep.show;
            activelyignored = efilter.Activelyignored;
            activelyselected = efilter.Activelyselected;
            activelywatched = false;
            privuser = efilter.privuser;
        }

        public bool Missed
        {
            get

            {
                return false;
            }
        }


        public FilterButtonsModel showFilter
        {
            get
            {
                if (ep == null)
                {
                    return null;

                }
                try
                {
                    return new FilterButtonsModel("show", showinclude, ep.Id);
                }
                catch
                {
                    return null;
                }
            }
        }
        public FilterButtonsModel networkFilter
        {
            get
            {
                if (ep == null || ep.Networks == null)
                {
                    return new FilterButtonsModel("network", networkinclude, (long)(-1));
                }

                return new FilterButtonsModel("network", networkinclude, (long)(ep.Networks.Id));
            }
        }
        public FilterButtonsModel webnetworkFilter
        {
            get
            {
                if (ep == null || ep.WebNetworks == null)
                {
                    return new FilterButtonsModel("webnetwork", webnetworkinclude, (long)(-1));
                }
                return new FilterButtonsModel("webnetwork", webnetworkinclude, (long)(ep.WebNetworks.Id));
            }
        }
        public FilterButtonsModel languageFilter
        {
            get
            {
                if (ep == null || ep.Languages == null)
                {
                    return new FilterButtonsModel("language", languageinclude, (long)(-1));
                }
                return new FilterButtonsModel("language", languageinclude, (long)(ep.Languages.Id));
            }
        }
        public FilterButtonsModel typeFilter
        {
            get
            {
                if (ep == null || ep.Types == null)
                {
                    return new FilterButtonsModel("type", typeinclude, (long)(-1));
                }
                return new FilterButtonsModel("type", typeinclude, (long)ep.Types.Id);
            }
        }
        public FilterButtonsModel genreFilter(int genreid, bool? include)
        {
            return new FilterButtonsModel("genre", include, (long)genreid);
        }

        public FilterButtonsModel countryFilter(int countryid, bool? include)
        {
            return new FilterButtonsModel("country", include, (long)countryid);
        }
    }


    public class EpFilter
    {
        public Episode ep;
        public bool privuser;
        private bool _activelyselected;
        private bool _activelyignored;
        public bool activelywatched;
        public bool? showinclude;
        public bool? networkinclude;
        public bool? webnetworkinclude;
        public bool? typeinclude;
        public bool? countryinclude;
        public bool? languageinclude;
        public bool? genreinclude;
        public int EpisodesBehind;
        public int TotalAiredEpisodes;
        public int TotalWatchedEpisodes;
        public int ShowPriority;
        public List<TVSite> TvSites;
        public EpFilter(List<TVSite> TVSites)
        {
            ep = null;
            _activelyignored = false;
            _activelyselected = false;
            activelywatched = false;
            privuser = false;
            TvSites = TVSites;
        }
        public EpFilter(Episode e, List<TVSite> TVSites)
        {
            ep = e;
            _activelyignored = false;
            _activelyselected = false;
            activelywatched = false;
            privuser = false;
            TvSites = TVSites;
        }
        public bool AlreadyDecidedUpon
        {
            get { return _activelyignored || _activelyselected; }
        }
        public bool Activelyignored
        {
            get { return _activelyignored; }
            set { _activelyignored = value; }
        }
        public bool Activelyselected
        {
            get { return _activelyselected; }
            set { _activelyselected = value; }
        }

        public bool Missed
        {
            get

            {
                if (ep == null)
                {
                    return false;
                }
                if (((DateTimeOffset)ep.AiringTime) < DateTimeOffset.Now.AddDays(-4))
                {
                    return true;
                }
                return false;
            }
        }


        public FilterButtonsModel showFilter
        {
            get
            {
                if (ep == null || ep.show == null)
                {
                    return null;

                }
                try
                {
                    return new FilterButtonsModel("show", showinclude, ep.show.Id);
                }
                catch
                {
                    return null;
                }
            }
        }
        public FilterButtonsModel networkFilter
        {
            get
            {
                if (ep == null || ep.show == null || ep.show.Networks == null)
                {
                    return new FilterButtonsModel("network", networkinclude, (long)(-1));
                }

                return new FilterButtonsModel("network", networkinclude, (long)(ep.show.Networks.Id));
            }
        }
        public FilterButtonsModel webnetworkFilter
        {
            get
            {
                if (ep == null || ep.show == null || ep.show.WebNetworks == null)
                {
                    return new FilterButtonsModel("webnetwork", webnetworkinclude, (long)(-1));
                }
                return new FilterButtonsModel("webnetwork", webnetworkinclude, (long)(ep.show.WebNetworks.Id));
            }
        }
        public FilterButtonsModel languageFilter
        {
            get
            {
                if (ep == null || ep.show == null || ep.show.Languages == null)
                {
                    return new FilterButtonsModel("language", languageinclude, (long)(-1));
                }
                return new FilterButtonsModel("language", languageinclude, (long)(ep.show.Languages.Id));
            }
        }
        public FilterButtonsModel typeFilter
        {
            get
            {
                if (ep == null || ep.show == null || ep.show.Types == null)
                {
                    return new FilterButtonsModel("type", typeinclude, (long)(-1));
                }
                return new FilterButtonsModel("type", typeinclude, (long)ep.show.Types.Id);
            }
        }
        public FilterButtonsModel genreFilter(int genreid, bool? include)
        {
            return new FilterButtonsModel("genre", include, (long)genreid);
        }

        public FilterButtonsModel countryFilter(int countryid, bool? include)
        {
            return new FilterButtonsModel("country", include, (long)countryid);
        }
    }


    public class FilterButtonsModel
    {

        public string _ItemType;
        public bool? _ItemStatus;
        public long _ItemKey;

        public FilterButtonsModel(string itemType, bool? itemStatus, long itemKey)
        {
            _ItemType = itemType;
            _ItemStatus = itemStatus;
            _ItemKey = itemKey;
        }

        public bool ShowPlus
        {
            get
            {
                if (_ItemKey < 0)
                {
                    return false;
                }
                if (_ItemStatus == null)
                {
                    return true;
                }
                return !(_ItemStatus ?? false);
            }
        }
        public bool ShowNegative
        {
            get
            {
                if (_ItemKey < 0)
                {
                    return false;
                }

                if (_ItemStatus == null)
                {
                    return true;
                }
                if (_ItemStatus ?? true) // Item is actively set
                {
                    return true;
                }
                return false;
            }
        }

    }
}
