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

        private async Task CopyFilesToFriends(List<FileInfo> filesToScan)
        {
            var friends = _db.Friends
                .Include(f => f.InterestedShows)
                    .ThenInclude(fs => fs.Show)
                .Where(f => !string.IsNullOrEmpty(f.FolderPath))
                .ToList();

            if (friends.Count == 0) return;

            var userShows = _db.Shows.Where(s => s.Wanted == true).ToList();

            foreach (var friend in friends)
            {
                var interestedShowIds = friend.InterestedShows.Select(fs => fs.ShowId).ToHashSet();
                if (interestedShowIds.Count == 0) continue;

                var alreadyCopied = _db.FriendCopies
                    .Where(c => c.FriendId == friend.Id)
                    .Select(c => c.FileName)
                    .ToHashSet();

                foreach (var fileinfo in filesToScan)
                {
                    if (alreadyCopied.Contains(fileinfo.Name)) continue;

                    var dirsplit = fileinfo.DirectoryName?.ToLower().Split(Path.DirectorySeparatorChar);
                    if (dirsplit == null || dirsplit.Length < 2) continue;
                    if (!dirsplit.Last().StartsWith("season ")) continue;

                    var showDir = dirsplit[dirsplit.Length - 2];
                    var seasonDir = dirsplit.Last();

                    var matchedShow = userShows.FirstOrDefault(u =>
                        (!string.IsNullOrEmpty(u.FolderName) && u.FolderName.ToLower().Trim() == showDir) ||
                        (!string.IsNullOrEmpty(u.name) && u.name.ToLower() == showDir) ||
                        u.DefaultFolderName.ToLower().Trim() == showDir);

                    if (matchedShow == null) continue;
                    if (!interestedShowIds.Contains(matchedShow.Id)) continue;

                    try
                    {
                        var showFolderName = matchedShow.FolderName ?? matchedShow.DefaultFolderName;
                        // Capitalise season folder properly e.g. "season 3" -> "Season 3"
                        var seasonFolderName = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                            .ToTitleCase(seasonDir);
                        var destDir = Path.Combine(friend.FolderPath!, showFolderName, seasonFolderName);

                        // Ensure the full destination path exists (root + show + season)
                        if (!Directory.Exists(friend.FolderPath))
                        {
                            _logger.LogInformation("Creating friend root folder '{FolderPath}' for '{FriendName}'",
                                friend.FolderPath, friend.Name);
                            Directory.CreateDirectory(friend.FolderPath!);
                        }
                        Directory.CreateDirectory(destDir);

                        var destFile = Path.Combine(destDir, fileinfo.Name);
                        if (!File.Exists(destFile))
                        {
                            File.Copy(fileinfo.FullName, destFile);
                            _logger.LogInformation("Copied '{FileName}' to friend '{FriendName}' at '{Dest}'",
                                fileinfo.Name, friend.Name, destFile);
                        }
                        _db.FriendCopies.Add(new FriendCopy
                        {
                            FriendId = friend.Id,
                            FileName = fileinfo.Name,
                            CopiedAt = DateTime.UtcNow
                        });
                        alreadyCopied.Add(fileinfo.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to copy '{FileName}' to friend '{FriendName}'",
                            fileinfo.Name, friend.Name);
                    }
                }
            }
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


        /// <summary>
        /// Resolves which show and episode a scanned file belongs to, given its show folder name.
        /// Handles continuation shows: a file under an old show's folder (e.g. "Foo/Season 3/...")
        /// can belong to a different show via a <see cref="ShowFolderAlias"/> with a SeasonOffset
        /// (showSeason = fileSeason - SeasonOffset). Direct (own-folder) matches win over
        /// continuation matches.
        /// </summary>
        /// <returns>
        /// matchedShow/matchedEpisode = the resolved owner (null episode when none found);
        /// directShow = the show matched purely by folder name (for logging);
        /// parsed = the parsed (season, episode) from the filename (null when unparseable).
        /// </returns>
        private (Show? matchedShow, Episode? matchedEpisode, Show? directShow, (long season, long episode)? parsed)
            ResolveShowEpisode(string showdir, string fileName, List<Show> userShows, List<ShowFolderAlias> aliases)
        {
            var key = showdir.ToLower().Trim();

            Show? directShow = userShows.FirstOrDefault(u => !string.IsNullOrEmpty(u.FolderName) && u.FolderName.ToLower().Trim() == key);
            if (directShow == null)
                directShow = userShows.FirstOrDefault(u => !string.IsNullOrEmpty(u.name) && u.name.ToLower() == key);
            if (directShow == null)
                directShow = userShows.FirstOrDefault(u => u.DefaultFolderName.ToLower().Trim() == key);

            var parsed = EpisodeNameParser.ParseFirst(fileName);
            if (parsed == null)
                return (null, null, directShow, null);

            var season = parsed.Value.season;
            var ep = parsed.Value.episode;

            // 1) Direct show at the parsed season wins.
            if (directShow != null)
            {
                var directEp = _db.Episodes.FirstOrDefault(e => e.show.Id == directShow.Id && e.number == ep && e.season == season);
                if (directEp != null)
                    return (directShow, directEp, directShow, parsed);
            }

            // 2) Continuation candidates: aliases whose name matches this folder, with a season offset.
            foreach (var alias in aliases.Where(a => a.Show != null
                         && !string.IsNullOrEmpty(a.AliasName)
                         && a.AliasName.ToLower().Trim() == key))
            {
                var effectiveSeason = season - alias.SeasonOffset;
                if (effectiveSeason < 1) continue;
                var contEp = _db.Episodes.FirstOrDefault(e => e.show.Id == alias.Show!.Id && e.number == ep && e.season == effectiveSeason);
                if (contEp != null)
                    return (alias.Show, contEp, directShow, parsed);
            }

            // Parsed fine, but no episode matched anywhere.
            return (directShow, null, directShow, parsed);
        }

        public async Task<bool> ShowDownloadedJob()
        {
            List<string> foundShowFolder = new List<string>(600000);
            List<TouchFile> foundShowFiles = new (600000);
            var UserShows = _db.Shows.Where(s => s.Wanted == true).ToList();
            var FolderAliases = _db.ShowFolderAliases.Include(a => a.Show).ToList();
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
            _logger.LogInformation("ShowDownloadedJob: {FileCount} files to scan", filesToScan.Count);
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

                        var (matchedShow, matchedEpisode, directShow, parsed) =
                            ResolveShowEpisode(showdir, fileinfo.Name, UserShows, FolderAliases);
                        show = matchedShow;
                        episode = matchedEpisode;

                        if (directShow == null)
                        {
                            _logger.LogWarning("ShowDownloadedJob: No show match for folder '{ShowDir}' (file: {FileName})", showdir, fileinfo.Name);
                        }
                        else if (parsed == null)
                        {
                            _logger.LogWarning("ShowDownloadedJob: Failed to parse episode from '{FileName}' for show '{ShowName}'", fileinfo.Name, directShow.name);
                        }
                        else if (episode == null)
                        {
                            _logger.LogWarning("ShowDownloadedJob: No episode found for {ShowName} S{Season}E{Episode} (file: {FileName})",
                                directShow.name, parsed.Value.season, parsed.Value.episode, fileinfo.Name);
                        }

                        if (show != null)
                        {
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

            await CopyFilesToFriends(filesToScan);

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
            var FolderAliases = _db.ShowFolderAliases.Include(a => a.Show).ToList();

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

                        var (matchedShow, matchedEpisode, _, _) =
                            ResolveShowEpisode(showdir, fileinfo.Name, UserShows, FolderAliases);
                        show = matchedShow;
                        episode = matchedEpisode;

                        if (show != null)
                        {
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

        /// <summary>
        /// Scans aliasable TV directories for folders matching show aliases.
        /// When an alias folder is found, renames it to the real show folder name.
        /// If the real folder already exists, merges contents (moves season/show sub-folders).
        /// </summary>
        public async Task<bool> ResolveAliasFolders()
        {
            try
            {
                var aliasableDirs = _db.TVDirectories
                    .Where(d => d.Aliasable)
                    .ToList();

                if (!aliasableDirs.Any())
                {
                    _logger.LogInformation("ResolveAliasFolders: No aliasable directories configured.");
                    return true;
                }

                var aliases = _db.ShowFolderAliases
                    .Include(a => a.Show)
                    .Where(a => a.Show != null)
                    .ToList();

                if (!aliases.Any())
                {
                    _logger.LogInformation("ResolveAliasFolders: No aliases defined.");
                    return true;
                }

                foreach (var tvDir in aliasableDirs)
                {
                    if (string.IsNullOrEmpty(tvDir.Name) || !Directory.Exists(tvDir.Name))
                    {
                        _logger.LogWarning("ResolveAliasFolders: Directory does not exist: {Dir}", tvDir.Name);
                        continue;
                    }

                    var rootPath = tvDir.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    foreach (var alias in aliases)
                    {
                        try
                        {
                            var realFolderName = alias.Show!.FolderName ?? alias.Show.DefaultFolderName;
                            if (string.IsNullOrEmpty(realFolderName)) continue;

                            var aliasPath = Path.Combine(rootPath, alias.AliasName);
                            if (!Directory.Exists(aliasPath)) continue;

                            var realPath = Path.Combine(rootPath, realFolderName);

                            _logger.LogInformation("ResolveAliasFolders: Found alias folder '{Alias}' -> '{Real}' (season offset {Offset}) in {Dir}",
                                alias.AliasName, realFolderName, alias.SeasonOffset, tvDir.Name);

                            // Always merge (even when the real folder doesn't exist yet) so the
                            // season offset gets applied consistently to "Season N" sub-folders.
                            Directory.CreateDirectory(realPath);
                            MergeDirectory(aliasPath, realPath, alias.SeasonOffset);
                            _logger.LogInformation("ResolveAliasFolders: Merged '{Alias}' into '{Real}'",
                                aliasPath, realPath);
                        }
                        catch (Exception ex)
                        {
                            // Don't let one bad alias (locked file, cross-volume move, permission error, etc.)
                            // block every other alias from being processed this run.
                            _logger.LogError(ex, "ResolveAliasFolders: Failed to resolve alias '{Alias}' in {Dir}",
                                alias.AliasName, tvDir.Name);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResolveAliasFolders failed");
                return false;
            }
        }

        private static readonly Regex SeasonFolderRegex = new(@"^(?<prefix>[Ss]eason)\s*(?<num>\d+)$");

        /// <summary>
        /// Recursively moves all contents from source into destination, then removes the empty source directory.
        /// When <paramref name="seasonOffset"/> is non-zero, "Season N" sub-folders directly under
        /// <paramref name="source"/> are renamed to "Season {N - seasonOffset}" to match the real show's
        /// season numbering (showSeason = fileSeason - SeasonOffset, see <see cref="ShowFolderAlias.SeasonOffset"/>).
        /// </summary>
        private void MergeDirectory(string source, string destination, int seasonOffset = 0)
        {
            // Move files
            foreach (var file in Directory.GetFiles(source))
            {
                var destFile = Path.Combine(destination, Path.GetFileName(file));
                if (!File.Exists(destFile))
                {
                    File.Move(file, destFile);
                }
                else
                {
                    _logger.LogWarning("ResolveAliasFolders: File already exists, skipping: {File}", destFile);
                }
            }

            // Move sub-directories (season folders)
            foreach (var dir in Directory.GetDirectories(source))
            {
                var dirName = Path.GetFileName(dir);
                var destDirName = dirName;

                if (seasonOffset != 0)
                {
                    var match = SeasonFolderRegex.Match(dirName);
                    if (match.Success && int.TryParse(match.Groups["num"].Value, out var seasonNum))
                    {
                        var mappedSeason = seasonNum - seasonOffset;
                        if (mappedSeason < 1)
                        {
                            _logger.LogWarning("ResolveAliasFolders: Skipping '{Dir}', season offset {Offset} produces invalid season {Mapped}",
                                dir, seasonOffset, mappedSeason);
                            continue;
                        }
                        destDirName = $"{match.Groups["prefix"].Value} {mappedSeason}";
                    }
                }

                var destDir = Path.Combine(destination, destDirName);

                if (!Directory.Exists(destDir))
                {
                    Directory.Move(dir, destDir);
                }
                else
                {
                    // Recursively merge sub-directory contents (offset already applied at this level)
                    MergeDirectory(dir, destDir);
                }
            }

            // Remove the now-empty alias folder
            if (!Directory.EnumerateFileSystemEntries(source).Any())
            {
                Directory.Delete(source);
            }
            else
            {
                _logger.LogWarning("ResolveAliasFolders: Source folder not empty after merge, skipping delete: {Dir}", source);
            }
        }

    }
}
