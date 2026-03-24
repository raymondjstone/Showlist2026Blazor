using System.Text.RegularExpressions;
using Flurl.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Showlist2026.Configuration;
using Showlist2026.Data;
using Showlist2026.Entities;
using Showlist2026.Models;
using Showlist2026.TVMaze.TVMazeEpisodes;
using Showlist2026.TVMaze.TVMazePage;
using Country = Showlist2026.Entities.Country;
using Network = Showlist2026.Entities.Network;
using Type = Showlist2026.Entities.Type;

namespace Showlist2026.Services
{
    public class ShowListBackgroundService : IShowListBackgroundService
    {
        private readonly ShowlistDbContext _db;
        private readonly ILogger<ShowListBackgroundService> _logger;
        private readonly ShowlistOptions _options;
        private readonly INotificationService _notifications;

        public ShowListBackgroundService(ShowlistDbContext db, ILogger<ShowListBackgroundService> logger,
            IOptions<ShowlistOptions> options, INotificationService notifications)
        {
            _db = db;
            _logger = logger;
            _options = options.Value;
            _notifications = notifications;
        }

        public HomePageStats HomePageStats()
        {
            HomePageStats hps = new HomePageStats();

            hps.shows = _db.Shows.Count();
            hps.episodes = _db.Episodes.Count();

            hps.watchedEpisodes = _db.Episodes.Count(e => e.Watched);

            return hps;
        }


        // Takes in a page number and processes the 250 shows per page
        public async Task<bool> RefreshNetworks()
        {
            List<Network> nlist = _db.Networks.ToList();
            List<Country> clist = _db.Countrys.ToList();

            long maxnet = 1800; // max networks in origin db at time of writing, used for initial load

            if (nlist.Any(a => a.networkid > maxnet))
            {
                maxnet = nlist.Max(a => a.networkid) + 50; //Allow for 50 new per path (the seq often jumps)
            }


            long i = 0;
                while (i < maxnet)
                {
                    i++;
                    try
                    {
                        TVMaze.TVMazePage.Network nwork = await ($"{_options.TvMazeBaseUrl}/networks/" + i.ToString())
                        .GetJsonAsync<TVMaze.TVMazePage.Network>();
                        Network s = nlist.FirstOrDefault(a => a.networkid == nwork.Id);
                        if (nwork.Country == null)
                        {
                            nwork.Country = new TVMaze.TVMazePage.Country(){Code="??", Name="Unknown", Timezone = "Unknown"};
                        }
                        Country c = clist.FirstOrDefault(a => a.code == nwork.Country.Code);

                        if (c == null)
                        {
                            c = new Country() { code = nwork.Country.Code, name = nwork.Country.Name };
                            clist.Add(c);
                        }


                    if (s == null)
                        {
                            Network snew = new Network()
                            {
                                networkid = nwork.Id,
                                name = nwork.Name,
                                timezone = nwork.Country.Timezone,
                                country = c

                            };
                            _db.Add(snew);
                        }
                        else
                        {
                            s.name = nwork.Name;
                            s.timezone = nwork.Country.Timezone;
                            if (s.country == null && nwork.Country != null)
                            {
                                s.country = c;
                            }
                            _db.Update(s);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "Failed to refresh network {NetworkId}", i);
                    }
                }


            await _db.SaveChangesAsync();
            return true;

        }
        public async Task<bool> RefreshWebNetworks()
        {
            return true;
        }


        public async Task<bool> RefreshShowEpisodes(Show show)
        {
            List<EpisodeData> eps= new List<EpisodeData>();
            try
            {
                eps = await ($"{_options.TvMazeBaseUrl}/shows/" + show.showid.ToString()+ "/episodes?specials=1").GetJsonAsync<List<EpisodeData>>();
            }
            catch (Exception e)
            {
                if (e.Message.Contains("code 429"))
                {
                    await Task.Delay(10000); //Rate limiting - wait, try again and add a pause

                    try
                    {
                        eps = await ($"{_options.TvMazeBaseUrl}/shows/" + show.showid.ToString() + "/episodes")
                            .GetJsonAsync<List<EpisodeData>>();
                    }
                    catch (Exception retryEx)
                    {
                        _logger.LogWarning(retryEx, "Retry failed for show {ShowId} episodes", show.showid);
                        return false;
                    }
                    await Task.Delay(5000);
                }
                else
                {
                    _logger.LogWarning(e, "Failed to fetch episodes for show {ShowId} ({ShowName})", show.showid, show.name);
                    return false;
                }
            }

            //Cull removed episodes
            if (show.Episodes == null)
            {
                show.Episodes = new List<Episode>();
            }

            List<Episode> showstodelete = new List<Episode>();
            foreach (Episode xe in show.Episodes)
            {
                if (!eps.Any(x => x.Id == xe.episodeid))
                {
                    showstodelete.Add(xe);
                }
            }

            foreach (Episode xe in showstodelete)
            {
                 show.Episodes.Remove(xe);
            }

            foreach (EpisodeData e in eps)
            {
                Episode dbe = show.Episodes.FirstOrDefault(a => a.episodeid == e.Id);
                if (dbe == null)
                {
                    dbe = new Episode();
                    show.Episodes.Add(dbe);
                }

                string med="";
                string orig = "";
                if (e.Image != null && e.Image.Medium != null)
                {
                    med = e.Image.Medium.ToString();
                }
                if (e.Image != null && e.Image.Original != null)
                {
                    orig = e.Image.Original.ToString();
                }

                try
                {
                    dbe.episodeid = e.Id;
                    dbe.name = e.Name;
                    dbe.imagemedium = med;
                    dbe.imageoriginal = orig;
                    dbe.summary = e.Summary;
                    dbe.links = e.Links?.Self?.Href;

                    dbe.season = e.Season;
                    dbe.number = e.Number??0;
                    dbe.airdate = e.Airdate;
                    dbe.airtime = e.Airtime?.ToString();
                    dbe.runtime = e.Runtime?.ToString();
                    try
                    {
                        if (!string.IsNullOrEmpty(dbe.airdate))
                        {
                            dbe.AirDateOffset2 = DateTimeOffset.Parse(dbe.airdate);
                            DateTimeOffset? oldd = dbe.AirDateOffset2;
                            try
                            {

                                if (!String.IsNullOrEmpty(dbe.airtime))
                                {
                                string[] s = dbe.airtime.Split(":");
                                if (s.Length == 2)
                                {
                                    int h = 0;
                                    int m = 0;
                                    int.TryParse(s[0], out h);
                                    int.TryParse(s[1], out m);
                                    m += (h * 60);
                                    dbe.AirDateOffset2 = dbe.AirDateOffset2?.AddMinutes(m);

                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                _logger.LogWarning(exception, "Failed to parse airtime for episode {EpisodeId}", e.Id);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Failed to parse airdate for episode {EpisodeId}", e.Id);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to update episode {EpisodeId} for show {ShowId}", e.Id, show.showid);
                }

                //show.Episodes.Add(dbe);
            }

            return true;
        }

        public async Task<bool> BacklogPage()
        {
            Show? s = _db.Shows.FirstOrDefault(a => a.needsupdate);
            if (s == null) return false;
            return await RefreshShowPage((int)s.page, (int)s.page);
        }

        [Serializable]
        public class Jobparams
        {
            public int Pageno { get; set; }
        }


        public async Task<bool> RefreshShowBatch()
        {
           long? pmax = _db.Shows.Max(a => a.page);
           var pmaxint = (int)(pmax?? 0);

            //TODO the page limit is not something that should be hardcoded.
            for (int pageno = 0; pageno < pmaxint+1; pageno++)
            {
                // TODO: Previously used background job manager to enqueue PageJob.
                // Now calling RefreshShowPage directly.
                await RefreshShowPage(pageno, pageno);
            }
            return true;
        }



        public async Task<bool> RefreshShowPage(int pageno)
        {
            return await RefreshShowPage(pageno, pageno);
        }

        // Takes in a page number and processes the 250 shows per page
        public async Task<bool> RefreshShowPage(int pagenofrom, int pagenoto)
        {

            List<Show> slist = _db.Shows
                .Where( s => s.page >= pagenofrom && s.page <= pagenoto)
                .Include(s => s.Types)
                .Include(s => s.Genres)
                .Include(s => s.WebNetworks)
                .Include(s => s.Networks)
                .Include(s => s.Networks.country)
                .Include(s => s.WebNetworks.country)
                .Include(s => s.Episodes)
                .ToList();
            List<Type> typelist = _db.Types.ToList();
            List<Timezone> timezonelist = _db.Timezones.ToList();
            List<Language> languagelist = _db.Languages.ToList();
            List<GenreText> genretextlist = _db.GenreTexts.ToList();
            List<Genre> genrelist = _db.Genres.ToList();
            List<Network> networklist = _db.Networks
                                .Include(s => s.country).ToList();
            List<WebNetwork> webnetworklist = _db.WebNetworks
                                .Include(s => s.country).ToList();
;
            List<Country> countrylist = _db.Countrys.ToList();


            while (pagenofrom <= pagenoto)
            {
                List<TVMazeShowData> shows;
                try
                {
                    shows = await ($"{_options.TvMazeBaseUrl}/shows?page=" + pagenofrom.ToString())
                        .GetJsonAsync<List<TVMazeShowData>>();
                }
                catch 
                {
                    shows = [];
                    //return false;
                }



                foreach (var su in shows)
                {
                    Show s = _db.Shows
                        .Where(s => s.showid == su.Id )
                .Include(s => s.Types)
                .Include(s => s.Genres)
                .Include(s => s.WebNetworks)
                .Include(s => s.Networks)
                .Include(s => s.Networks.country)
                .Include(s => s.WebNetworks.country)
                .Include(s => s.Episodes)
                .ToList().FirstOrDefault();


                    if (s == null || s.needsupdate)
                    {


                        if (su.Network == null)
                        {
                            su.Network = new TVMaze.TVMazePage.Network() {Id = -1, Name = "", Country = null};
                        }

                        if (su.WebChannel == null)
                        {
                            su.WebChannel = new TVMaze.TVMazePage.Network() {Id = -1, Name = "", Country = null};
                        }


                        if (su.Network.Country == null)
                        {
                            su.Network.Country = new TVMaze.TVMazePage.Country()
                                {Code = "??", Name = "Unknown", Timezone = "Unknown"};
                        }

                        if (su.WebChannel.Country == null)
                        {
                            su.WebChannel.Country = new TVMaze.TVMazePage.Country()
                                {Code = "??", Name = "Unknown", Timezone = "Unknown"};
                        }


                        if (su.Network.Country.Timezone == null)
                        {
                            Timezone t1 =
                                timezonelist.FirstOrDefault(a => a.countrycode == su.Network.Country.Code);

                            if (t1 == null)
                            {
                                t1 =
                                    timezonelist.FirstOrDefault(a => a.timezone == "Unknown");
                            }

                            su.Network.Country.Timezone = t1.timezone;
                        }
                        if (su.WebChannel.Country.Timezone == null)
                        {
                            Timezone t1 =
                                timezonelist.FirstOrDefault(a => a.countrycode == su.WebChannel.Country.Code);

                            if (t1 == null)
                            {
                                t1 =
                                    timezonelist.FirstOrDefault(a => a.timezone == "Unknown");
                            }

                            su.WebChannel.Country.Timezone = t1.timezone;
                        }



                        Country c = countrylist.FirstOrDefault(a => a.code == su.Network.Country.Code);
                        if (c == null)
                        {
                            c = new Country() {code = su.Network.Country.Code, name = su.Network.Country.Name};
                            countrylist.Add(c);
                        }

                        Network nw = networklist.FirstOrDefault(a => a.networkid == su.Network.Id);
                        if (nw == null)
                        {
                            nw = new Network()
                            {
                                networkid = su.Network.Id, name = su.Network.Name,
                                timezone = su.Network.Country.Timezone,
                                country = c,
                                tz = timezonelist.First(x => x.timezone == su.Network.Country.Timezone)
                            };
                            networklist.Add(nw);
                        }

                        WebNetwork wnw = webnetworklist.FirstOrDefault(a => a.webid == su.WebChannel.Id);
                        if (wnw == null)
                        {
                            wnw = new WebNetwork()
                            {
                                webid = su.WebChannel.Id, name = su.WebChannel.Name,
                                timezone = su.WebChannel.Country.Timezone, country = c,
                                tz = timezonelist.First(x => x.timezone == su.Network.Country.Timezone)
                            };
                            webnetworklist.Add(wnw);
                        }

                        List<Genre> thisgenre = new List<Genre>();
                        if (su.Genres != null)
                        {
                            foreach (var gx in su.Genres)
                            {
                                Genre g = null;
                                GenreText gt = genretextlist.FirstOrDefault(a => a.genre == gx);
                                if (gt == null)
                                {
                                    gt = new GenreText() { genre = gx };
                                    genretextlist.Add(gt);
                                    _db.Add(gt);
                                    _db.SaveChanges();
                                }

                                if (s != null)
                                {
                                    g = genrelist.FirstOrDefault(a => a.show != null && a.genretext != null && a.genretext.Id == gt.Id && a.show.Id == s.Id);
                                }
                                if (g == null)
                                {
                                    g = new Genre() { genretext = gt, show = s };
                                    genrelist.Add(g);
                                }

                                thisgenre.Add(g);
                            }
                        }

                        Type t = typelist.FirstOrDefault(a => a.type == su.Type);
                        if (t == null)
                        {
                            t = new Type() {type = su.Type};
                            typelist.Add(t);
                        }

                        Language l = languagelist.FirstOrDefault(a => a.name == su.Language);
                        if (l == null)
                        {
                            l = new Language() {name = su.Language};
                            languagelist.Add(l);
                        }

                        string url = "";
                        string med = "";
                        string orig = "";
                        if (su.Url != null)
                        {
                            url = su.Url.ToString();
                        }

                        if (su.Image != null && su.Image.Medium != null)
                        {
                            med = su.Image.Medium.ToString();
                        }

                        if (su.Image != null && su.Image.Original != null)
                        {
                            orig = su.Image.Original.ToString();
                        }

                        string days = "";
                        string comma = "";
                        if (su.Schedule != null && su.Schedule.Days != null)
                        {
                            foreach (var d in su.Schedule.Days)
                            {
                                days += comma + d;
                                comma = ",";
                            }
                        }

                        //Show s = slist.FirstOrDefault(a => a.showid == su.Id);
                        bool isNewShow = s == null;
                        if (s == null)
                        {
                            s = new Show()
                            {
                                showid = su.Id,
                                page = pagenofrom,
                                name = su.Name,
                                status = su.Status,
                                updated = su.Updated.ToString(),
                                needsupdate = true,
                                Networks = nw, WebNetworks = wnw,
                                summary = su.Summary,
                                Genres = thisgenre,
                                Types = t,
                                Languages = l,
                                url = url,
                                scheduletime = su.Schedule?.Time,
                                scheduledays = days,
                                premiered = su.Premiered?.ToString(),
                                imagemed = med,
                                imageorig = orig,
                            };
                            // Fix up genre references now that the show exists
                            foreach (var g in thisgenre)
                            {
                                g.show = s;
                            }

                            //Externals contains keys to match other systems
                            if (su.Externals != null)
                            {
                                try
                                {
                                    s.tvrage = su.Externals.Tvrage?.ToString();
                                    s.imdb = su.Externals.Imdb;
                                    s.thetvdb = su.Externals.Thetvdb?.ToString();

                                    if (string.IsNullOrEmpty(s.tvrage)) s.tvrage = null;
                                    if (string.IsNullOrEmpty(s.thetvdb)) s.thetvdb = null;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to parse externals for show {ShowId}", su.Id);
                                }
                            }


                        }

                        if (s.needsupdate)
                        {
                            // Remove old genres to prevent duplicates (skip for new shows)
                            if (!isNewShow && s.Genres != null && s.Genres.Any())
                            {
                                _db.Genres.RemoveRange(s.Genres);
                            }

                            s.name = su.Name;
                            s.status = su.Status;
                            s.updated = su.Updated.ToString();
                            s.page = pagenofrom;
                            s.Networks = nw;
                            s.WebNetworks = wnw;
                            s.summary = su.Summary;
                            s.Genres = thisgenre;
                            s.Types = t;
                            s.Languages = l;

                            s.url = url;
                            s.scheduletime = su.Schedule?.Time;
                            s.scheduledays = days;
                            s.premiered = su.Premiered?.ToString();
                            s.imagemed = med;
                            s.imageorig = orig;

                            //Externals contains keys to match other systems
                            if (su.Externals != null)
                            {
                                try
                                {
                                    s.tvrage = su.Externals.Tvrage?.ToString();
                                    s.imdb = su.Externals.Imdb;
                                    s.thetvdb = su.Externals.Thetvdb?.ToString();

                                    if (string.IsNullOrEmpty(s.tvrage)) s.tvrage = null;
                                    if (string.IsNullOrEmpty(s.thetvdb)) s.thetvdb = null;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to parse externals for show {ShowId}", su.Id);
                                }
                            }


                            var epsok = await RefreshShowEpisodes(s);
                            if (epsok)
                            {
                                s.needsupdate = false;
                            }

                            _db.Update(s);
                        }
                    }
                }

                pagenofrom++;

            }

            await _db.SaveChangesAsync();
            return true;

        }


        public async Task<bool> RefreshShows()
        {
            try
            {
                List<Show> slist = _db.Shows
                    .Where(s => s.needsupdate).Take(10000)
                    .Include(s => s.Types)
                    .Include(s => s.Genres)
                    .Include(s => s.WebNetworks)
                    .Include(s => s.Networks)
                    .Include(s => s.Networks.country)
                    .Include(s => s.WebNetworks.country)
                    .Include(s => s.Episodes)
                    .ToList();

                List<Type> typelist = _db.Types.ToList();
                List<Timezone> timezonelist = _db.Timezones.ToList();
                List<Language> languagelist = _db.Languages.ToList();
                List<Genre> genrelist = _db.Genres.ToList();
                List<GenreText> genretextlist = _db.GenreTexts.ToList();
                List<Network> networklist = _db.Networks.ToList();
                List<WebNetwork> webnetworklist = _db.WebNetworks.ToList();
                List<Country> countrylist = _db.Countrys.ToList();


                foreach (Show s in slist.Where(a => a.needsupdate).OrderByDescending(s => s.updated).Take(10000))
                {
                    TVMazeShowData su = new TVMazeShowData();
                    try
                    {
                        su = await ($"{_options.TvMazeBaseUrl}/shows/" + s.showid.ToString())
                            .GetJsonAsync<TVMazeShowData>();
                    }
                    catch 
                    {
                        su = null;
                        //return false;
                    }

                    if (su != null && su.Id != 0)
                    {


                        if (s == null || s.needsupdate)
                        {

                            if (su.Network == null)
                            {
                                su.Network = new TVMaze.TVMazePage.Network() {Id = -1, Name = "", Country = null};
                            }

                            if (su.WebChannel == null)
                            {
                                su.WebChannel = new TVMaze.TVMazePage.Network() {Id = -1, Name = "", Country = null};
                            }


                            if (su.Network.Country == null)
                            {
                                su.Network.Country = new TVMaze.TVMazePage.Country()
                                    {Code = "??", Name = "Unknown", Timezone = "Unknown"};
                            }

                            if (su.WebChannel.Country == null)
                            {
                                su.WebChannel.Country = new TVMaze.TVMazePage.Country()
                                    {Code = "??", Name = "Unknown", Timezone = "Unknown"};
                            }
                            if (su.Network.Country.Timezone == null)
                            {
                                Timezone t1 = getTimezone(timezonelist, su.Network.Country.Code);
                                su.Network.Country.Timezone = t1.timezone;
                            }
                            if (su.WebChannel.Country.Timezone == null)
                            {
                                Timezone t1 = getTimezone(timezonelist, su.WebChannel.Country.Code);
                                su.WebChannel.Country.Timezone = t1.timezone;
                            }


                            Country c = countrylist.FirstOrDefault(a => a.code == su.Network.Country.Code);
                            if (c == null)
                            {
                                c = new Country() {code = su.Network.Country.Code, name = su.Network.Country.Name};
                                countrylist.Add(c);
                            }

                            Network nw = networklist.FirstOrDefault(a => a.networkid == su.Network.Id);
                            if (nw == null)
                            {
                                Timezone t1 = getTimezone(timezonelist, su.Network.Country.Code);

                                nw = new Network()
                                {
                                    networkid = su.Network.Id,
                                    name = su.Network.Name,
                                    timezone = su.Network.Country.Timezone,
                                    country = c,
                                    tz = t1

                                };
                                networklist.Add(nw);
                            }

                            WebNetwork wnw = webnetworklist.FirstOrDefault(a => a.webid == su.WebChannel.Id);
                            if (wnw == null)
                            {
                                Timezone t1 = getTimezone(timezonelist, su.WebChannel.Country.Code);
                                wnw = new WebNetwork()
                                {
                                    webid = su.WebChannel.Id,
                                    name = su.WebChannel.Name,
                                    timezone = su.WebChannel.Country.Timezone,
                                    country = c,
                                    tz = t1

                                };
                                webnetworklist.Add(wnw);
                            }

                            List<Genre> thisgenre = new List<Genre>();
                            if (su.Genres != null)
                            {
                                foreach (var gx in su.Genres)
                                {
                                    Genre g = null;
                                    GenreText gt = genretextlist.FirstOrDefault(a => a.genre == gx);
                                    if (gt == null)
                                    {
                                        gt = new GenreText() {genre = gx};
                                        genretextlist.Add(gt);
                                        _db.Add(gt);
                                        _db.SaveChanges();
                                    }

                                    if (s != null)
                                    {
                                        g = genrelist.FirstOrDefault(a =>
                                            a.show != null && a.genretext != null && a.genretext.Id == gt.Id && a.show.Id == s.Id);
                                    }

                                    if (g == null)
                                    {
                                        g = new Genre() {genretext = gt, show = s};
                                        genrelist.Add(g);
                                    }

                                    thisgenre.Add(g);
                                }
                            }

                            Type t = typelist.FirstOrDefault(a => a.type == su.Type);
                            if (t == null)
                            {
                                t = new Type() {type = su.Type};
                                typelist.Add(t);
                            }

                            Language l = languagelist.FirstOrDefault(a => a.name == su.Language);
                            if (l == null)
                            {
                                l = new Language() {name = su.Language};
                                languagelist.Add(l);
                            }

                            string url = "";
                            string med = "";
                            string orig = "";
                            if (su.Url != null)
                            {
                                url = su.Url.ToString();
                            }

                            if (su.Image != null && su.Image.Medium != null)
                            {
                                med = su.Image.Medium.ToString();
                            }

                            if (su.Image != null && su.Image.Original != null)
                            {
                                orig = su.Image.Original.ToString();
                            }

                            string days = "";
                            string comma = "";
                            if (su.Schedule != null && su.Schedule.Days != null)
                            {
                                foreach (var d in su.Schedule.Days)
                                {
                                    days += comma + d;
                                    comma = ",";
                                }
                            }


                            if (s.needsupdate)
                            {
                                // Remove old genres to prevent duplicates
                                if (s.Genres != null && s.Genres.Any())
                                {
                                    _db.Genres.RemoveRange(s.Genres);
                                }

                                s.name = su.Name;
                                s.status = su.Status;
                                s.updated = su.Updated.ToString();
                                s.page = s.page;
                                s.Networks = nw;
                                s.WebNetworks = wnw;
                                s.summary = su.Summary;
                                s.Genres = thisgenre;
                                s.Types = t;
                                s.Languages = l;

                                s.url = url;
                                s.scheduletime = su.Schedule.Time.ToString();
                                s.scheduledays = days;
                                s.premiered = su.Premiered?.ToString();
                                s.imagemed = med;
                                s.imageorig = orig;

                                //Externals contains keys to match other systems
                                if (su.Externals != null)
                                {
                                    try
                                    {
                                        s.tvrage = su.Externals.Tvrage?.ToString();
                                        s.imdb = su.Externals.Imdb;
                                        s.thetvdb = su.Externals.Thetvdb?.ToString();

                                        if (string.IsNullOrEmpty(s.tvrage)) s.tvrage = null;
                                        if (string.IsNullOrEmpty(s.thetvdb)) s.thetvdb = null;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to parse externals for show {ShowId}", su.Id);
                                    }
                                }


                                var epsok = await RefreshShowEpisodes(s);
                                if (epsok && su.Name != null)
                                {
                                    s.needsupdate = false;
                                }

                                _db.Update(s);
                            }
                        }
                    }

                }

                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError("Refresh shows job failed " + e.Message);
            }

            return true;

        }



        public async Task<bool> PopulateShowFolderNames()
        {
            try
            {
                List<Show> slist = _db.Shows
                    .Where(s => String.IsNullOrEmpty(s.FolderName))
                    .Include(s => s.WebNetworks)
                    .Include(s => s.Networks)
                    .Include(s => s.Networks.country)
                    .Include(s => s.WebNetworks.country)
                    .ToList();

                List<Timezone> timezonelist = _db.Timezones.ToList();
                List<Language> languagelist = _db.Languages.ToList();
                List<Network> networklist = _db.Networks.ToList();
                List<WebNetwork> webnetworklist = _db.WebNetworks.ToList();
                List<Country> countrylist = _db.Countrys.ToList();

                string rootfolder = _options.TvNameListPath;

                foreach (Show s in slist.Where(sh => sh.Wanted == true).OrderByDescending(s => s.updated))
                {
                    Console.WriteLine($"{s.name}  - {s.DefaultFolderName}");
                    string foundname = null;

                    if (networklist.Any())
                    {
                        foreach (var n in networklist)
                        {
                            if (n.country != null)
                            {
                                if (!String.IsNullOrEmpty(n.country.code))
                                {
                                    var attemptedname = $"{s.DefaultFolderName} {n.country.code}";
                                    Console.WriteLine($"{@rootfolder}{@attemptedname}");
                                    if (Directory.Exists($"{@rootfolder}{@attemptedname}"))
                                    {
                                        foundname = attemptedname;
                                    }
                                }
                            }
                        }
                    }

                    if (foundname == null && s.ShowStart.Year > 1960)
                    {
                        var attemptedname = $"{s.DefaultFolderName} {s.ShowStart.Year}";
                        Console.WriteLine($"{@rootfolder}{@attemptedname}");
                        if (Directory.Exists($"{@rootfolder}{@attemptedname}"))
                        {
                             foundname = attemptedname;
                        }
                    }

                    if (webnetworklist.Any())
                    {
                        foreach (var n in webnetworklist)
                        {
                            if (n.country != null)
                            {
                                if (!String.IsNullOrEmpty(n.country.code))
                                {
                                    var attemptedname = $"{s.DefaultFolderName} {n.country.code}";
                                    Console.WriteLine($"{@rootfolder}{@attemptedname}");
                                    if (Directory.Exists($"{@rootfolder}{@attemptedname}"))
                                    {
                                        foundname = attemptedname;
                                    }
                                }
                            }
                        }
                    }



                    if (foundname == null)
                    {
                        var attemptedname = $"{s.DefaultFolderName}";
                        Console.WriteLine($"{@rootfolder}{@attemptedname}");
                        if (Directory.Exists($"{@rootfolder}{@attemptedname}"))
                        {
                            foundname = attemptedname;
                        }
                    }

                    if (!String.IsNullOrEmpty(foundname))
                    {
                        s.FolderName = foundname.ToString();
                        _db.Update(s);
                    }


                }

            }
            catch (Exception e)
            {
                _logger.LogError("FolderName for shows job failed " + e.Message);
            }
            await _db.SaveChangesAsync();
            return true;
        }


        public int GetEstimatedPageMax()
        {
            double x = (double)_db.Shows.Max(a => a.showid);
            x = Math.Floor(x / 250);
            return (int) (x + 1);
        }

        public async Task<bool> RefreshShowDates()
        {
            try
            {
            Dictionary<string, long> showupdates = await $"{_options.TvMazeBaseUrl}/updates/shows".GetJsonAsync<Dictionary<string, long>>();

            List<Show> slist = _db.Shows.ToList();
            foreach (var su in showupdates)
            {
                long sux = long.Parse(su.Key);
                Show s = slist.FirstOrDefault(a => a.showid == sux);
                double p = Math.Floor((double)sux / 250);

                if (s != null)
                {
                    if (s.updated != su.Value.ToString() || s.page != p)
                    {
                        s.page = (long) p;
                        s.updated = su.Value.ToString();
                        s.needsupdate = true;
                    }
                }
                else
                {
                    Show snew = new Show()
                    {
                        page = (long)p,
                        showid =  sux,
                        updated = su.Value.ToString(),
                        needsupdate = true,
                };
                    _db.Add(snew);
                }
            }
            }
            catch (Exception e)
            {
                _logger.LogError("Refresh show dates failed " + e.Message);
            }

            await _db.SaveChangesAsync();
            return true;

        }


        public async Task<bool> Notificationtest()
        {
            await _notifications.SendAsync("Showlist2026 Test", "This is a test notification from Showlist2026.");
            return true;
        }

        private async Task<List<FileInfo>> Dirlist(string dirName, int daysOldToAllow, string filter = "*.*", int minSizeAllowed = 50000)
        {
            List<FileInfo> filesList = new List<FileInfo>();
            DateTime oldest = DateTime.Now.AddDays(0 - daysOldToAllow);
            try
            {
                var files = Directory.GetFiles(dirName, filter, SearchOption.AllDirectories).ToList();
                foreach (var f in files)
                {
                    var fi = new FileInfo(f);
                    if (daysOldToAllow < 0 || fi.LastWriteTime >= oldest)
                    {
                        if (fi.Length >= minSizeAllowed)
                        {
                            filesList.Add(fi);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan directory {DirName}", dirName);
            }
            return filesList.OrderByDescending(f => f.LastWriteTime).ToList();

        }


        public async Task<bool> ShowDownloadedJob()
        {
            List<string> foundShowFolder = new List<string>(600000);
            List<TouchFile> foundShowFiles = new (600000);
            var UserShows = _db.Shows.Where(s => s.Wanted == true).ToList();
            var dirs = _db.TVDirectories
                .Where(d => d.DaysToScan != 0)
                .OrderByDescending(d =>d.MinFileSize)
                .ThenByDescending(d=>d.DaysToScan);

            List<FileInfo> filesToScan = new List<FileInfo>();
            try
            {
                foreach (var tvdir in dirs)
                {
                    var _dirlist = await Dirlist(@tvdir.Name.Trim(), tvdir.DaysToScan, tvdir.Filter, tvdir.MinFileSize);
                    filesToScan.AddRange(_dirlist);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to scan TV directories");
            }
            int filenum = 0;
            foreach (var f in filesToScan)
            {
                filenum++;
                var fileinfo = f;
#if DEBUG
                Console.WriteLine($"{filenum} of {filesToScan.Count} {fileinfo.Name}");
#endif

                var dirsplit = fileinfo.DirectoryName.ToLower().Split(Path.DirectorySeparatorChar);
                var showFolderName = dirsplit.Last();
                if (dirsplit.Length >= 2 && dirsplit.Last().ToLower().StartsWith("season "))
                {
                    showFolderName = dirsplit[dirsplit.Length - 2].ToLower();
                }

                if (!foundShowFolder.Contains(showFolderName))
                {
                    foundShowFolder.Add(showFolderName);
                }

                bool updateTouchrecord = false;
                var tf = _db.TouchFiles
                    .FirstOrDefault(a => a.Name == fileinfo.Name);
                if (tf is null)
                {
                    tf = foundShowFiles.FirstOrDefault(a => a.Name == fileinfo.Name);
                    if (tf is null)
                    {
                        tf = new TouchFile();
                        tf.Name = fileinfo.Name;
                        tf.FileDate = fileinfo.CreationTimeUtc;
                        tf.WasRealFile = (fileinfo.Length > 200);
                        updateTouchrecord = true;
                        foundShowFiles.Add(tf);
                    }
                }

                var tfprev = tf.WasRealFile;
                tf.WasRealFile = (tf.WasRealFile || fileinfo.Length > 200);

                if (tfprev != tf.WasRealFile)
                {
                    updateTouchrecord = true;
                }


                Episode episode = null;
                Show show = null;
                bool alreadyWatched = false;
                if (fileinfo != null && fileinfo.DirectoryName != null && fileinfo.DirectoryName.Length > 5)
                {
                    if (dirsplit.Length >= 2 && dirsplit.Last().ToLower().StartsWith("season "))
                    {
                        String showdir = dirsplit[dirsplit.Length - 2].ToLower();
                        String seasondir = dirsplit.Last().ToLower();
                        show = UserShows.FirstOrDefault(u => !string.IsNullOrEmpty(u.FolderName) && u.FolderName.ToLower().Trim() == showdir.ToLower().Trim());
                        if (show == null)
                        {
                            show = UserShows.FirstOrDefault(u => !string.IsNullOrEmpty(u.name) && u.name.ToLower() == showdir.ToLower());
                        }

                        // If no show then no point in parsing any more
                        if (show != null)
                        {
                            var parsed = EpisodeNameParser.Parse(fileinfo.Name);
                            if (parsed != null)
                            {
                                episode = _db.Episodes.FirstOrDefault(e => e.show.Id == show.Id && e.number == parsed.Value.episode
                                && e.season == parsed.Value.season);
                            }

                            if (episode != null)
                            {
                                if (tf.Episode is null)
                                {
                                    updateTouchrecord = true;
                                }
                                tf.Episode = episode;

                                alreadyWatched = episode.Watched;
                            }

                            if (episode != null && !alreadyWatched)
                            {

                                string Titletxt = $"{@show.FolderName??show.name}";
                                string Messagetxt = $"{@episode.EpNumberFormatted} {@episode.name}";
                                try
                                {
                                    await _notifications.SendAsync(Titletxt, Messagetxt);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogWarning(e, "Failed to send download notification for {ShowName}", show.name);
                                }
                                try
                                {
                                    //This will set the episode to being watched
                                    episode.Watched = true;
                                    if (episode.GivenUp) episode.GivenUp = false;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to add watched selection for episode {EpisodeId}", episode.Id);
                                }

                            }
                        }
                    }
                }
                if (updateTouchrecord)
                {
                    if (tf.Id == 0)
                    {
                        _db.Add(tf);
                    }
                    else
                    {
                        _db.Update(tf);
                    }
                }
            }


            foreach (var f in foundShowFolder)
            {
                var tfolder = _db.TouchFolder
                    .FirstOrDefault(a => a.Name == f);
                if (tfolder is null)
                {
                    tfolder = new TouchFolder();
                    tfolder.Name = f;
                    tfolder.FileDate = DateTime.UtcNow;
                    _db.Add(tfolder);
                }
            }

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ScanDirectoryFull(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Directory path is required.");

            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Directory not found: {directory}");

            List<string> foundShowFolder = new List<string>(600000);
            List<TouchFile> foundShowFiles = new(600000);
            var UserShows = _db.Shows.Where(s => s.Wanted == true).ToList();

            var filesToScan = await Dirlist(directory.Trim(), -1, "*.*", 0);

            int filenum = 0;
            foreach (var f in filesToScan)
            {
                filenum++;
                var fileinfo = f;
#if DEBUG
                Console.WriteLine($"{filenum} of {filesToScan.Count} {fileinfo.Name}");
#endif

                var dirsplit = fileinfo.DirectoryName.ToLower().Split(Path.DirectorySeparatorChar);
                var showFolderName = dirsplit.Last();
                if (dirsplit.Length >= 2 && dirsplit.Last().ToLower().StartsWith("season "))
                {
                    showFolderName = dirsplit[dirsplit.Length - 2].ToLower();
                }

                if (!foundShowFolder.Contains(showFolderName))
                {
                    foundShowFolder.Add(showFolderName);
                }

                bool updateTouchrecord = false;
                var tf = _db.TouchFiles
                    .FirstOrDefault(a => a.Name == fileinfo.Name);
                if (tf is null)
                {
                    tf = foundShowFiles.FirstOrDefault(a => a.Name == fileinfo.Name);
                    if (tf is null)
                    {
                        tf = new TouchFile();
                        tf.Name = fileinfo.Name;
                        tf.FileDate = fileinfo.CreationTimeUtc;
                        tf.WasRealFile = (fileinfo.Length > 200);
                        updateTouchrecord = true;
                        foundShowFiles.Add(tf);
                    }
                }

                var tfprev = tf.WasRealFile;
                tf.WasRealFile = (tf.WasRealFile || fileinfo.Length > 200);

                if (tfprev != tf.WasRealFile)
                {
                    updateTouchrecord = true;
                }

                Episode episode = null;
                Show show = null;
                bool alreadyWatched = false;
                if (fileinfo != null && fileinfo.DirectoryName != null && fileinfo.DirectoryName.Length > 5)
                {
                    if (dirsplit.Length >= 2 && dirsplit.Last().ToLower().StartsWith("season "))
                    {
                        String showdir = dirsplit[dirsplit.Length - 2].ToLower();
                        String seasondir = dirsplit.Last().ToLower();
                        show = UserShows.FirstOrDefault(u => !string.IsNullOrEmpty(u.FolderName) && u.FolderName.ToLower().Trim() == showdir.ToLower().Trim());
                        if (show == null)
                        {
                            show = UserShows.FirstOrDefault(u => !string.IsNullOrEmpty(u.name) && u.name.ToLower() == showdir.ToLower());
                        }

                        if (show != null)
                        {
                            var parsed = EpisodeNameParser.Parse(fileinfo.Name);
                            if (parsed != null)
                            {
                                episode = _db.Episodes.FirstOrDefault(e => e.show.Id == show.Id && e.number == parsed.Value.episode
                                && e.season == parsed.Value.season);
                            }

                            if (episode != null)
                            {
                                if (tf.Episode is null)
                                {
                                    updateTouchrecord = true;
                                }
                                tf.Episode = episode;

                                alreadyWatched = episode.Watched;
                            }

                            if (episode != null && !alreadyWatched)
                            {
                                string Titletxt = $"{@show.FolderName ?? show.name}";
                                string Messagetxt = $"{@episode.EpNumberFormatted} {@episode.name}";
                                try
                                {
                                    await _notifications.SendAsync(Titletxt, Messagetxt);
                                }
                                catch (Exception e)
                                {
                                    _logger.LogWarning(e, "Failed to send download notification for {ShowName}", show.name);
                                }
                                try
                                {
                                    episode.Watched = true;
                                    if (episode.GivenUp) episode.GivenUp = false;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to add watched selection for episode {EpisodeId}", episode.Id);
                                }
                            }
                        }
                    }
                }
                if (updateTouchrecord)
                {
                    if (tf.Id == 0)
                    {
                        _db.Add(tf);
                    }
                    else
                    {
                        _db.Update(tf);
                    }
                }
            }

            foreach (var f in foundShowFolder)
            {
                var tfolder = _db.TouchFolder
                    .FirstOrDefault(a => a.Name == f);
                if (tfolder is null)
                {
                    tfolder = new TouchFolder();
                    tfolder.Name = f;
                    tfolder.FileDate = DateTime.UtcNow;
                    _db.Add(tfolder);
                }
            }

            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RecheckTouchFiles()
        {
            List<string> foundShowFolder = new List<string>(600000);
            List<TouchFile> foundShowFiles = new(600000);
            var UserShows = _db.Shows.Where(s => s.Wanted == true).ToList();

            var filesToScan = _db.TouchFiles
                .Include(s => s.Episode)
                .Where(a => a.FileDate > DateTime.Now.AddDays(-365))
                .ToList()
                .Where(a => a.Episode is null);

            foreach (var f in filesToScan)
            {
            }

            return true;
        }

        private Timezone getTimezone(List<Timezone> timezonelist, string countrycode)
        {
            Timezone t1 =
                timezonelist.FirstOrDefault(a => a.countrycode == countrycode);

            if (t1 == null)
            {
                t1 =
                    timezonelist.FirstOrDefault(a => a.timezone == "Unknown");
            }
            return t1;
        }


    }
}
