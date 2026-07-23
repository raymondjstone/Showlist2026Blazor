using System.Text.Json;
using System.Text.RegularExpressions;
using Flurl.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Showlist2026.Configuration;
using Showlist2026.Data;
using Showlist2026.Entities;
using Showlist2026.Models;
using Showlist2026.NZBPlanetApiJSON;
using Showlist2026.TVMaze;
using Country = Showlist2026.Entities.Country;
using Network = Showlist2026.Entities.Network;
using Type = Showlist2026.Entities.Type;

namespace Showlist2026.Services
{
    public class ShowListAppService : IShowListAppService
    {
        private readonly IDbContextFactory<ShowlistDbContext> _dbFactory;
        private readonly ILogger<ShowListAppService> _logger;
        private readonly ShowlistOptions _options;
        private readonly INotificationService _notifications;


        public ShowListAppService(IDbContextFactory<ShowlistDbContext> dbFactory, ILogger<ShowListAppService> logger,
            IOptions<ShowlistOptions> options, INotificationService notifications)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _options = options.Value;
            _notifications = notifications;
        }

        public List<Show> showlist(string srch)
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.Shows.Where(s => s.name.Contains(srch)).OrderBy(a => a.name).AsNoTracking().ToList();
        }

        public List<TVSite> TvSites()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.TVSites.OrderBy(a => a.Order).AsNoTracking().ToList();
        }

        public async Task TVSiteUpdate(int id, bool active, int order, string name, string urltemplate, 
            string apiKey = "", string apiBaseUrl = "", string rssApiKey = "", string rssBaseUrl = "")
        {
            using var _db = _dbFactory.CreateDbContext();
            TVSite current = null;
            if (id > 0)
            {
                current = _db.TVSites.Find(id);
            }

            if (current == null)
            {
                var newOne = new TVSite()
                {
                    Order = order,
                    Name = name,
                    URLTemplate = @urltemplate,
                    Active = active,
                    ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
                    ApiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? null : apiBaseUrl,
                    RssApiKey = string.IsNullOrWhiteSpace(rssApiKey) ? null : rssApiKey,
                    RssBaseUrl = string.IsNullOrWhiteSpace(rssBaseUrl) ? null : rssBaseUrl
                };
                _db.Add(newOne);
            }
            else
            {
                current.Order = order;
                current.Name = name;
                current.URLTemplate = @urltemplate;
                current.Active = active;
                current.ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
                current.ApiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? null : apiBaseUrl;
                current.RssApiKey = string.IsNullOrWhiteSpace(rssApiKey) ? null : rssApiKey;
                current.RssBaseUrl = string.IsNullOrWhiteSpace(rssBaseUrl) ? null : rssBaseUrl;
                _db.Update(current);
            }

            await _db.SaveChangesAsync();


        }

        public async Task TVSiteDelete(int id)
        {
            using var _db = _dbFactory.CreateDbContext();
            var site = _db.TVSites.Find(id);
            if (site != null)
            {
                _db.TVSites.Remove(site);
                await _db.SaveChangesAsync();
            }
        }

        public List<TVDirectories> TvDirectories()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.TVDirectories.OrderBy(a => a.Name).ToList();
        }

        public async Task TVDirectoryUpdate(int id, string name, int daysToScan, string filter, int minFileSize, bool aliasable = false)
        {
            using var _db = _dbFactory.CreateDbContext();
            TVDirectories current = null;
            if (id > 0)
            {
                current = _db.TVDirectories.Find(id);
            }

            if (current == null)
            {
                var newOne = new TVDirectories()
                {
                    Name = name,
                    DaysToScan = daysToScan,
                    Filter = filter,
                    MinFileSize = minFileSize,
                    Aliasable = aliasable
                };
                _db.Add(newOne);
            }
            else
            {
                current.Name = name;
                current.DaysToScan = daysToScan;
                current.Filter = filter;
                current.MinFileSize = minFileSize;
                current.Aliasable = aliasable;
                _db.Update(current);
            }

            await _db.SaveChangesAsync();
        }

        public async Task TVDirectoryDelete(int id)
        {
            using var _db = _dbFactory.CreateDbContext();
            var dir = _db.TVDirectories.Find(id);
            if (dir != null)
            {
                _db.TVDirectories.Remove(dir);
                await _db.SaveChangesAsync();
            }
        }

        public List<Country> CountryData()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.Countrys.ToList() ?? new List<Country>();
        }

        public List<Language> LanguageData()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.Languages.ToList() ?? new List<Language>();
        }

        public List<Type> TypeData()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.Types.ToList() ?? new List<Type>();
        }

        public List<GenreText> GenreData()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.GenreTexts.ToList() ?? new List<GenreText>();
        }

        public List<Network> NetworkData()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.Networks.Include(n => n.country).ToList() ?? new List<Network>();
        }

        public List<WebNetwork> WebNetworkData()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.WebNetworks.Include(n => n.country).ToList() ?? new List<WebNetwork>();
        }

        public List<Show> ShowData()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.Shows.Where(s => s.Wanted != null).OrderBy(s => s.name).AsNoTracking().ToList();
        }



        public Show ShowPageData(long id)
        {
            using var _db = _dbFactory.CreateDbContext();

            try
            {
                var s = _db.Shows
                    .Where(a => a.Id == (int)id)
                    .Include(s => s.Types)
                    .Include(s => s.Genres)
                    .Include(s => s.WebNetworks)
                    .Include(s => s.Networks)
                    .Include(s => s.Networks.country)
                    .Include(s => s.WebNetworks.country)
                    .Include(s => s.Episodes)
                    ;

                if (s == null || s.Count() < 1)
                {
                    return null;
                }

                var show = s.First();

                // These loads populate the EF change tracker so navigation properties resolve
                var temp = _db.GenreTexts.ToList();
                var tz = _db.Timezones.ToList();

                PopulateSuggestedFolderNames(_db, new List<Show> { show });
                return show;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ShowPageData failed for id={id}");
                return null;
            }


        }

        public List<EpFilter> AiringAroundNowForUser(int daysminus = -15, int daysplus = 15, bool firstshowOnly = false, bool includeIgnored = false, bool includeWatched = false)
        {
            using var _db = _dbFactory.CreateDbContext();
            var sw = System.Diagnostics.Stopwatch.StartNew();


            //Used to use a view but that ended up being too restrictive
            DateTimeOffset min = DateTimeOffset.UtcNow.AddDays(daysminus);
            DateTimeOffset max = DateTimeOffset.UtcNow.AddDays(daysplus);

            if (_db.Database.IsRelational())
                _db.Database.SetCommandTimeout(120);

            // Load all user filter selections first (small datasets, fast queries)
            var showFilters = _db.Shows.Where(s => s.Wanted != null)
                .Select(s => new { s.Id, Include = s.Wanted.Value })
                .ToDictionary(s => s.Id, s => s.Include);
            var maps = LoadFilterMaps(_db);
            _logger.LogDebug($"PERF[AiringAroundNow] User filters loaded: {sw.ElapsedMilliseconds}ms");

            // Load all show FK IDs in ONE lightweight query for filter matching
            var allShowFks = _db.Shows
                .Select(s => new {
                    s.Id,
                    NetworkId = s.Networks != null ? (int?)s.Networks.Id : null,
                    WebNetworkId = s.WebNetworks != null ? (int?)s.WebNetworks.Id : null,
                    TypeId = s.Types != null ? (int?)s.Types.Id : null,
                    LanguageId = s.Languages != null ? (int?)s.Languages.Id : null,
                    NetCountryId = s.Networks != null && s.Networks.country != null ? (int?)s.Networks.country.Id : null,
                    WebCountryId = s.WebNetworks != null && s.WebNetworks.country != null ? (int?)s.WebNetworks.country.Id : null
                }).ToList();
            _logger.LogDebug($"PERF[AiringAroundNow] Show FKs loaded ({allShowFks.Count} shows): {sw.ElapsedMilliseconds}ms");

            // Load genre-to-show mapping in one query
            var genreShowMap = _db.Genres
                .Where(g => g.genretext != null && g.show != null)
                .Select(g => new { ShowId = g.show.Id, GenreTextId = g.genretext.Id })
                .ToList();
            _logger.LogDebug($"PERF[AiringAroundNow] Genre map loaded ({genreShowMap.Count} entries): {sw.ElapsedMilliseconds}ms");

            // Build the set of relevant show IDs in memory from user's positive selections
            var selectedNetworkIds = maps.Network.Where(n => n.Value).Select(n => n.Key).ToHashSet();
            var selectedWebNetworkIds = maps.WebNetwork.Where(n => n.Value).Select(n => n.Key).ToHashSet();
            var selectedTypeIds = maps.Type.Where(t => t.Value).Select(t => t.Key).ToHashSet();
            var selectedLangIds = maps.Language.Where(l => l.Value).Select(l => l.Key).ToHashSet();
            var selectedCountryIds = maps.Country.Where(c => c.Value).Select(c => c.Key).ToHashSet();
            var selectedGenreTextIds = maps.Genre.Where(g => g.Value).Select(g => g.Key).ToHashSet();

            var relevantShowIds = new HashSet<int>();

            // Shows directly selected
            foreach (var sf in showFilters.Where(s => s.Value))
                relevantShowIds.Add(sf.Key);

            // Match shows against all filter types in a single pass
            foreach (var s in allShowFks)
            {
                if (s.NetworkId.HasValue && selectedNetworkIds.Contains(s.NetworkId.Value)) { relevantShowIds.Add(s.Id); continue; }
                if (s.WebNetworkId.HasValue && selectedWebNetworkIds.Contains(s.WebNetworkId.Value)) { relevantShowIds.Add(s.Id); continue; }
                if (s.TypeId.HasValue && selectedTypeIds.Contains(s.TypeId.Value)) { relevantShowIds.Add(s.Id); continue; }
                if (s.LanguageId.HasValue && selectedLangIds.Contains(s.LanguageId.Value)) { relevantShowIds.Add(s.Id); continue; }
                if (s.NetCountryId.HasValue && selectedCountryIds.Contains(s.NetCountryId.Value)) { relevantShowIds.Add(s.Id); continue; }
                if (s.WebCountryId.HasValue && selectedCountryIds.Contains(s.WebCountryId.Value)) { relevantShowIds.Add(s.Id); continue; }
            }

            // Match genres
            if (selectedGenreTextIds.Count > 0)
                foreach (var g in genreShowMap)
                    if (selectedGenreTextIds.Contains(g.GenreTextId))
                        relevantShowIds.Add(g.ShowId);

            _logger.LogDebug($"PERF[AiringAroundNow] Relevant shows computed ({relevantShowIds.Count} shows): {sw.ElapsedMilliseconds}ms");

            // Query 1: Episodes for user's selected shows (full date range)
            // No .Include(Genres) - it's a collection that causes massive split queries. Loaded separately below.
            var relevantShowIdsList = relevantShowIds.ToList();
            var selectedEps = _db.Episodes
                .Where(a => a.AirDateOffset2 >= min && a.AirDateOffset2 <= max
                && (firstshowOnly == false ||
                    (a.number == 1 && a.season == 1)
                    )
                && relevantShowIdsList.Contains(a.show.Id)
                )
                .Include(s => s.show)
                .Include(s => s.show.Languages)
                .Include(s => s.show.Types)
                .Include(s => s.show.WebNetworks)
                .Include(s => s.show.Networks)
                .ToList();
            _logger.LogDebug($"PERF[AiringAroundNow] Selected show episodes loaded ({selectedEps.Count} eps): {sw.ElapsedMilliseconds}ms");

            // Query 2: S01E01 "new show" discovery.
            // Never look further back than the caller's own window (`min`), otherwise narrow
            // ranges like TonightsEpisodes(0,0) would surface 90 days of premieres. Cap the
            // look-back at 90 days for performance when the requested window is wider than that.
            var ninetyDaysAgo = DateTimeOffset.UtcNow.AddDays(-90);
            var recentMin = min > ninetyDaysAgo ? min : ninetyDaysAgo;
            var newShowEps = _db.Episodes
                .Where(a => a.AirDateOffset2 >= recentMin && a.AirDateOffset2 <= max
                && a.number == 1 && a.season == 1
                && !relevantShowIdsList.Contains(a.show.Id)
                )
                .Include(s => s.show)
                .Include(s => s.show.Languages)
                .Include(s => s.show.Types)
                .Include(s => s.show.WebNetworks)
                .Include(s => s.show.Networks)
                .ToList();
            _logger.LogDebug($"PERF[AiringAroundNow] New show S01E01 loaded ({newShowEps.Count} eps): {sw.ElapsedMilliseconds}ms");

            // Query 3: Episodes for ignored/excluded shows (only when includeIgnored is true, e.g. calendar)
            var ignoredEps = new List<Episode>();
            if (includeIgnored)
            {
                var ignoredShowIds = showFilters
                    .Where(s => !s.Value)
                    .Select(s => s.Key)
                    .Where(id => !relevantShowIds.Contains(id))
                    .ToList();
                if (ignoredShowIds.Count > 0)
                {
                    ignoredEps = _db.Episodes
                        .Where(a => a.AirDateOffset2 >= min && a.AirDateOffset2 <= max
                            && ignoredShowIds.Contains(a.show.Id))
                        .Include(s => s.show)
                        .Include(s => s.show.Languages)
                        .Include(s => s.show.Types)
                        .Include(s => s.show.WebNetworks)
                        .Include(s => s.show.Networks)
                        .ToList();
                }
                _logger.LogDebug($"PERF[AiringAroundNow] Ignored show episodes loaded ({ignoredEps.Count} eps): {sw.ElapsedMilliseconds}ms");
            }

            var eps = selectedEps.Concat(newShowEps).Concat(ignoredEps).ToList();
            _logger.LogDebug($"PERF[AiringAroundNow] Total episodes ({eps.Count} eps): {sw.ElapsedMilliseconds}ms");

            // Filter out watched episodes in memory
            if (!includeWatched)
                eps = eps.Where(e => !e.Watched).ToList();
            _logger.LogDebug($"PERF[AiringAroundNow] After watched filter ({eps.Count} eps): {sw.ElapsedMilliseconds}ms");

            // Attach genres to shows in memory using the genreShowMap we already loaded
            var showIds = eps.Where(e => e.show != null).Select(e => e.show.Id).Distinct().ToHashSet();
            var genresForShows = _db.Genres
                .Include(g => g.genretext)
                .Where(g => g.show != null && showIds.Contains(g.show.Id))
                .ToList();
            var genresByShowId = genresForShows.GroupBy(g => g.show.Id)
                .ToDictionary(g => g.Key, g => (ICollection<Genre>)g.ToList());
            foreach (var ep in eps.Where(e => e.show != null))
            {
                if (genresByShowId.TryGetValue(ep.show.Id, out var genres))
                    ep.show.Genres = genres;
            }
            _logger.LogDebug($"PERF[AiringAroundNow] Genres attached: {sw.ElapsedMilliseconds}ms");

            // These loads populate the EF change tracker so navigation properties (tz, genretext) resolve
            _db.Timezones.ToList();
            _db.GenreTexts.ToList();

            var tvsites = TvSites();
            var showFilterMap = showFilters;

            List<EpFilter> EpFilters = new List<EpFilter>(eps.Count);
            foreach (var e in eps.Where(a => a.show != null))
            {
                var ef = CreateEpFilter(e, showFilterMap, maps, tvsites);
                ef.activelywatched = e.Watched;
                EpFilters.Add(ef);
            }

            // Watched episodes already excluded at DB level
            IEnumerable<EpFilter> filtered;
            if (includeIgnored)
            {
                filtered = EpFilters;
            }
            else
            {
                filtered = EpFilters.Where(i =>
                    i.Activelyselected ||
                        (!i.Activelyignored && i.ep.number == 1 && i.ep.season == 1) //Not actively selected so only include S01E01 for speed reasons
                );
            }

            var result = filtered.ToList();
            _logger.LogDebug($"PERF[AiringAroundNow] TOTAL: {sw.ElapsedMilliseconds}ms | {result.Count} results from {eps.Count} episodes, daysminus={daysminus}, daysplus={daysplus}, firstshowOnly={firstshowOnly}");
            return result;


        }



        private void PopulateSuggestedFolderNames(ShowlistDbContext _db, List<Show> shows)
        {
            // Find show names that exist more than once in the DB
            var names = shows.Where(s => !string.IsNullOrEmpty(s.name)).Select(s => s.name).Distinct().ToList();
            var duplicateNames = _db.Shows
                .Where(s => names.Contains(s.name))
                .GroupBy(s => s.name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();

            foreach (var s in shows)
            {
                if (!string.IsNullOrEmpty(s.name) && duplicateNames.Contains(s.name) && s.ShowStart.Year > 1900)
                    s.SuggestedFolderName = $"{s.DefaultFolderName} {s.ShowStart.Year}";
                else
                    s.SuggestedFolderName = s.DefaultFolderName;
            }
        }

        private sealed class FilterMaps
        {
            public required Dictionary<int, bool> Network { get; init; }
            public required Dictionary<int, bool> WebNetwork { get; init; }
            public required Dictionary<int, bool> Genre { get; init; }
            public required Dictionary<int, bool> Language { get; init; }
            public required Dictionary<int, bool> Type { get; init; }
            public required Dictionary<int, bool> Country { get; init; }
        }

        // Loads the user's include/exclude selections for every filterable entity in one place.
        private static FilterMaps LoadFilterMaps(ShowlistDbContext db) => new()
        {
            Network = db.Networks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value),
            WebNetwork = db.WebNetworks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value),
            Genre = db.GenreTexts.Where(g => g.Wanted != null).ToDictionary(g => g.Id, g => g.Wanted.Value),
            Language = db.Languages.Where(l => l.Wanted != null).ToDictionary(l => l.Id, l => l.Wanted.Value),
            Type = db.Types.Where(t => t.Wanted != null).ToDictionary(t => t.Id, t => t.Wanted.Value),
            Country = db.Countrys.Where(c => c.Wanted != null).ToDictionary(c => c.Id, c => c.Wanted.Value),
        };

        private EpFilter CreateEpFilter(
                Episode e,
                Dictionary<int, bool> showFilterMap,
                FilterMaps maps,
                List<TVSite> tvsites
            )
        {
            EpFilter ef = new EpFilter(e, tvsites);



                if (showFilterMap.TryGetValue(ef.ep.show.Id, out var showInc))
                {
                    ef.showinclude = showInc;
                    if (showInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!showInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.Types != null && maps.Type.TryGetValue(ef.ep.show.Types.Id, out var typeInc))
                {
                    ef.typeinclude = typeInc;
                    if (typeInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!typeInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.Networks != null && maps.Network.TryGetValue(ef.ep.show.Networks.Id, out var netInc))
                {
                    ef.networkinclude = netInc;
                    if (netInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!netInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.WebNetworks != null && maps.WebNetwork.TryGetValue(ef.ep.show.WebNetworks.Id, out var webInc))
                {
                    ef.webnetworkinclude = webInc;
                    if (webInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!webInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.Languages != null && maps.Language.TryGetValue(ef.ep.show.Languages.Id, out var langInc))
                {
                    ef.languageinclude = langInc;
                    if (langInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!langInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                //Country filters checked against both main Network and WebNetwork in turn
                if (ef.ep.show.Networks?.country != null && maps.Country.TryGetValue(ef.ep.show.Networks.country.Id, out var cntInc))
                {
                    ef.countryinclude = cntInc;
                    if (cntInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!cntInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }
                if (ef.ep.show.WebNetworks?.country != null && maps.Country.TryGetValue(ef.ep.show.WebNetworks.country.Id, out var wcntInc))
                {
                    if (ef.countryinclude == null) ef.countryinclude = wcntInc;
                    if (wcntInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!wcntInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.Genres != null)
                {
                    foreach (var g in ef.ep.show.Genres)
                    {
                        if (g.genretext != null && maps.Genre.TryGetValue(g.genretext.Id, out var gInc))
                        {
                            if (ef.genreinclude == null) ef.genreinclude = gInc;
                            if (gInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                            if (!gInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                        }
                    }
                }

            return ef;
        }



        public List<EpFilter> UndecidedShows()
        {
            using var _db = _dbFactory.CreateDbContext();
            var sw = System.Diagnostics.Stopwatch.StartNew();


            // Shows the user has already made a decision on (wanted or unwanted)
            var decidedShowIds = _db.Shows
                .Where(s => s.Wanted != null)
                .Select(s => s.Id)
                .ToHashSet();
            _logger.LogDebug($"PERF[UndecidedShows] Decided shows loaded ({decidedShowIds.Count}): {sw.ElapsedMilliseconds}ms");

            // Load user filter exclusions directly from main entity tables
            var maps = LoadFilterMaps(_db);

            // Build sets of excluded IDs from filters
            var excludedNetworkIds = maps.Network.Where(n => !n.Value).Select(n => n.Key).ToHashSet();
            var excludedWebNetworkIds = maps.WebNetwork.Where(n => !n.Value).Select(n => n.Key).ToHashSet();
            var excludedTypeIds = maps.Type.Where(t => !t.Value).Select(t => t.Key).ToHashSet();
            var excludedLangIds = maps.Language.Where(l => !l.Value).Select(l => l.Key).ToHashSet();
            var excludedCountryIds = maps.Country.Where(c => !c.Value).Select(c => c.Key).ToHashSet();
            var excludedGenreTextIds = maps.Genre.Where(g => !g.Value).Select(g => g.Key).ToHashSet();
            _logger.LogDebug($"PERF[UndecidedShows] Filters loaded: {sw.ElapsedMilliseconds}ms");

            // Load all show FK IDs for filter matching
            var allShowFks = _db.Shows
                .Select(s => new {
                    s.Id,
                    NetworkId = s.Networks != null ? (int?)s.Networks.Id : null,
                    WebNetworkId = s.WebNetworks != null ? (int?)s.WebNetworks.Id : null,
                    TypeId = s.Types != null ? (int?)s.Types.Id : null,
                    LanguageId = s.Languages != null ? (int?)s.Languages.Id : null,
                    NetCountryId = s.Networks != null && s.Networks.country != null ? (int?)s.Networks.country.Id : null,
                    WebCountryId = s.WebNetworks != null && s.WebNetworks.country != null ? (int?)s.WebNetworks.country.Id : null
                }).ToList();

            // Genre-to-show mapping
            var genreShowMap = _db.Genres
                .Where(g => g.genretext != null && g.show != null)
                .Select(g => new { ShowId = g.show.Id, GenreTextId = g.genretext.Id })
                .ToList();

            // Build set of shows excluded by genre
            var genreExcludedShowIds = new HashSet<int>();
            if (excludedGenreTextIds.Count > 0)
                foreach (var g in genreShowMap)
                    if (excludedGenreTextIds.Contains(g.GenreTextId))
                        genreExcludedShowIds.Add(g.ShowId);

            // Filter: undecided shows that are NOT excluded by any filter
            var eligibleShowIds = new HashSet<int>();
            foreach (var s in allShowFks)
            {
                if (decidedShowIds.Contains(s.Id)) continue;
                if (s.NetworkId.HasValue && excludedNetworkIds.Contains(s.NetworkId.Value)) continue;
                if (s.WebNetworkId.HasValue && excludedWebNetworkIds.Contains(s.WebNetworkId.Value)) continue;
                if (s.TypeId.HasValue && excludedTypeIds.Contains(s.TypeId.Value)) continue;
                if (s.LanguageId.HasValue && excludedLangIds.Contains(s.LanguageId.Value)) continue;
                if (s.NetCountryId.HasValue && excludedCountryIds.Contains(s.NetCountryId.Value)) continue;
                if (s.WebCountryId.HasValue && excludedCountryIds.Contains(s.WebCountryId.Value)) continue;
                if (genreExcludedShowIds.Contains(s.Id)) continue;
                eligibleShowIds.Add(s.Id);
            }
            _logger.LogDebug($"PERF[UndecidedShows] Eligible shows computed ({eligibleShowIds.Count}): {sw.ElapsedMilliseconds}ms");

            // Get S01E01 episodes for eligible shows
            var eligibleShowIdsList = eligibleShowIds.ToList();
            var eps = _db.Episodes
                .Where(a => a.number == 1 && a.season == 1
                    && a.AirDateOffset2 < DateTimeOffset.UtcNow
                    && eligibleShowIdsList.Contains(a.show.Id))
                .Include(s => s.show)
                .Include(s => s.show.Languages)
                .Include(s => s.show.Types)
                .Include(s => s.show.WebNetworks)
                .Include(s => s.show.Networks)
                .ToList();
            _logger.LogDebug($"PERF[UndecidedShows] Episodes loaded ({eps.Count}): {sw.ElapsedMilliseconds}ms");

            // Load the latest episode per show so the card can display first + last
            // Use raw SQL for efficient per-group-max pattern
            var showIdsWithEps = eps.Where(e => e.show != null).Select(e => e.show.Id).Distinct().ToList();
            if (showIdsWithEps.Count > 0)
            {
                var idsCsv = string.Join(",", showIdsWithEps);
                var lastEps = _db.Episodes
                    .FromSqlRaw($"SELECT e.* FROM Episode e INNER JOIN (SELECT showId, MAX(Id) AS MaxId FROM Episode WHERE showId IN ({idsCsv}) GROUP BY showId) m ON e.Id = m.MaxId")
                    .ToList();
                _logger.LogDebug($"PERF[UndecidedShows] Last episodes loaded ({lastEps.Count}): {sw.ElapsedMilliseconds}ms");

                // Attach to EF change tracker so show navigation resolves
                var lastEpByShowId = new Dictionary<int, Episode>();
                foreach (var le in lastEps)
                {
                    var entry = _db.Entry(le);
                    var showIdFk = entry.Property<int?>("showId").CurrentValue;
                    if (showIdFk.HasValue)
                        lastEpByShowId[showIdFk.Value] = le;
                }
                foreach (var ep in eps.Where(e => e.show != null))
                {
                    ep.show.Episodes ??= new List<Episode> { ep };
                    if (lastEpByShowId.TryGetValue(ep.show.Id, out var last) && last.Id != ep.Id)
                        ep.show.Episodes.Add(last);
                }
            }

            // Exclude watched episodes
            eps = eps.Where(e => !e.Watched).ToList();

            // Attach genres separately
            var showIds = eps.Where(e => e.show != null).Select(e => e.show.Id).Distinct().ToHashSet();
            var genresForShows = _db.Genres
                .Include(g => g.genretext)
                .Where(g => g.show != null && showIds.Contains(g.show.Id))
                .ToList();
            var genresByShowId = genresForShows.GroupBy(g => g.show.Id)
                .ToDictionary(g => g.Key, g => (ICollection<Genre>)g.ToList());
            foreach (var ep in eps.Where(e => e.show != null))
            {
                if (genresByShowId.TryGetValue(ep.show.Id, out var genres))
                    ep.show.Genres = genres;
            }

            // Populate EF change tracker for timezone resolution in AiringTime
            _db.Timezones.ToList();
            _db.GenreTexts.ToList();

            var tvsites = TvSites();

            // Build EpFilters - these are all undecided, so none will be Activelyselected or Activelyignored
            var showFilterMap = new Dictionary<int, bool>();

            List<EpFilter> EpFilters = new List<EpFilter>(eps.Count);
            foreach (var e in eps.Where(a => a.show != null))
            {
                EpFilters.Add(
                    CreateEpFilter(e, showFilterMap, maps, tvsites)
                );
            }

            var result = EpFilters.ToList();
            _logger.LogDebug($"PERF[UndecidedShows] TOTAL: {sw.ElapsedMilliseconds}ms | {result.Count} results");
            return result;
        }

        public List<EpFilter> NextUnwatchedPerShow()
        {
            using var _db = _dbFactory.CreateDbContext();
            var sw = System.Diagnostics.Stopwatch.StartNew();


            // Get all wanted show IDs
            var wantedShowIds = _db.Shows
                .Where(s => s.Wanted == true)
                .Select(s => s.Id)
                .ToHashSet();

            // Get all unwatched episodes for wanted shows that have already aired
            var wantedShowIdsList = wantedShowIds.ToList();
            var unwatchedEps = _db.Episodes
                .Where(a => a.AirDateOffset2 < DateTimeOffset.UtcNow
                    && !a.Watched
                    && wantedShowIdsList.Contains(a.show.Id))
                .Include(s => s.show)
                .Include(s => s.show.Languages)
                .Include(s => s.show.Types)
                .Include(s => s.show.WebNetworks)
                .Include(s => s.show.Networks)
                .ToList();
            _logger.LogDebug($"PERF[NextUnwatched] Unwatched eps loaded ({unwatchedEps.Count}): {sw.ElapsedMilliseconds}ms");

            // Group by show, pick earliest unwatched per show, count total behind
            var grouped = unwatchedEps
                .Where(e => e.show != null)
                .GroupBy(e => e.show.Id)
                .Select(g => new {
                    NextEp = g.OrderBy(e => e.season).ThenBy(e => e.number).First(),
                    BehindCount = g.Count()
                })
                .OrderByDescending(x => x.BehindCount)
                .ToList();

            // Attach genres
            var showIds = grouped.Select(g => g.NextEp.show.Id).Distinct().ToHashSet();
            var genresForShows = _db.Genres
                .Include(g => g.genretext)
                .Where(g => g.show != null && showIds.Contains(g.show.Id))
                .ToList();
            var genresByShowId = genresForShows.GroupBy(g => g.show.Id)
                .ToDictionary(g => g.Key, g => (ICollection<Genre>)g.ToList());
            foreach (var item in grouped.Where(x => x.NextEp.show != null))
            {
                if (genresByShowId.TryGetValue(item.NextEp.show.Id, out var genres))
                    item.NextEp.show.Genres = genres;
            }

            _db.Timezones.ToList();
            _db.GenreTexts.ToList();

            var tvsites = TvSites();
            var showFilterMap = _db.Shows.Where(s => s.Wanted != null)
                .ToDictionary(s => s.Id, s => s.Wanted.Value);

            // Get total aired episode counts per show and watched counts
            var airedCountsByShow = _db.Episodes
                .Where(e => e.AirDateOffset2 < DateTimeOffset.UtcNow && wantedShowIdsList.Contains(e.show.Id))
                .GroupBy(e => e.show.Id)
                .Select(g => new { ShowId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ShowId, x => x.Count);

            var watchedCountsByShow = _db.Episodes
                .Where(e => (e.Watched || e.GivenUp) && wantedShowIdsList.Contains(e.show.Id))
                .GroupBy(e => e.show.Id)
                .Select(g => new { ShowId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ShowId, x => x.Count);

            var givenUpCountsByShow = _db.Episodes
                .Where(e => e.GivenUp && wantedShowIdsList.Contains(e.show.Id))
                .GroupBy(e => e.show.Id)
                .Select(g => new { ShowId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ShowId, x => x.Count);

            // Get priorities
            var priorityMap = _db.Shows
                .Where(s => s.Wanted == true && s.Priority > 0)
                .ToDictionary(s => s.Id, s => s.Priority);

            List<EpFilter> result = new List<EpFilter>();
            foreach (var item in grouped)
            {
                var ef = new EpFilter(item.NextEp, tvsites);
                if (showFilterMap.TryGetValue(item.NextEp.show.Id, out var inc))
                    ef.showinclude = inc;
                ef.Activelyselected = true;
                ef.EpisodesBehind = item.BehindCount;
                airedCountsByShow.TryGetValue(item.NextEp.show.Id, out var aired);
                watchedCountsByShow.TryGetValue(item.NextEp.show.Id, out var watched);
                ef.TotalAiredEpisodes = aired;
                ef.TotalWatchedEpisodes = watched;
                givenUpCountsByShow.TryGetValue(item.NextEp.show.Id, out var givenUp);
                ef.TotalGivenUpEpisodes = givenUp;
                priorityMap.TryGetValue(item.NextEp.show.Id, out var prio);
                ef.ShowPriority = prio;
                result.Add(ef);
            }

            _logger.LogDebug($"PERF[NextUnwatched] TOTAL: {sw.ElapsedMilliseconds}ms | {result.Count} shows");
            return result;
        }

        public List<Show> NoFolderList()
        {
            using var _db = _dbFactory.CreateDbContext();
            var x = _db.Shows
                    .Where(s => s.Wanted == true)
                   .OrderBy(s => s.name);

            var results = x.Where(s => string.IsNullOrEmpty(s.FolderName)).ToList();
            PopulateSuggestedFolderNames(_db, results);
            return results;
        }


        public List<ShowFilter> ComingSoonForUser(int daysminus = 1, int daysplus = 366)
        {
            using var _db = _dbFactory.CreateDbContext();


            //Used to use a view but that ended up being too restrictive
            DateTimeOffset min = DateTimeOffset.UtcNow.AddDays(daysminus);
            DateTimeOffset max = DateTimeOffset.UtcNow.AddDays(daysplus);

            // premiered is stored ISO-formatted ("yyyy-MM-dd"), so match on a leading year rather
            // than the "/yyyy" substring this used to look for (which real TVMaze dates never
            // contain, silently making this pre-filter match nothing).
            string yeara = min.Year.ToString();
            string yearb = max.Year.ToString();

            var eps1 = _db.Shows.Where(a => a.premiered != null && (a.premiered.StartsWith(yeara) || a.premiered.StartsWith(yearb))).ToList();

            var eps = eps1.Where(a => a.ShowStart >= min && a.ShowStart <= max).ToList();

            // These loads populate the EF change tracker so navigation properties resolve
            var temp1 = _db.Languages.ToList();
            var temp2 = _db.Types.ToList();
            var temp3 = _db.Genres.ToList();
            var temp4 = _db.WebNetworks.ToList();
            var temp5 = _db.Networks.ToList();
            var temp7 = _db.Countrys.ToList();

            var showFilterMap = _db.Shows.Where(s => s.Wanted != null)
                .ToDictionary(s => s.Id, s => s.Wanted.Value);
            var maps = LoadFilterMaps(_db);

            List<ShowFilter> EpFilters = new List<ShowFilter>(eps.Count);
            foreach (var e in eps)
            {
                ShowFilter ef = new ShowFilter(e);

                bool decided = false;
                if (showFilterMap.TryGetValue(ef.ep.Id, out var showInc))
                {
                    ef.showinclude = showInc;
                    if (showInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.Types != null && maps.Type.TryGetValue(ef.ep.Types.Id, out var typeInc))
                {
                    ef.typeinclude = typeInc;
                    if (typeInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.Networks != null && maps.Network.TryGetValue(ef.ep.Networks.Id, out var netInc))
                {
                    ef.networkinclude = netInc;
                    if (netInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.WebNetworks != null && maps.WebNetwork.TryGetValue(ef.ep.WebNetworks.Id, out var webInc))
                {
                    ef.webnetworkinclude = webInc;
                    if (webInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.Languages != null && maps.Language.TryGetValue(ef.ep.Languages.Id, out var langInc))
                {
                    ef.languageinclude = langInc;
                    if (langInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.Networks?.country != null && maps.Country.TryGetValue(ef.ep.Networks.country.Id, out var cntInc))
                {
                    ef.countryinclude = cntInc;
                    if (cntInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }
                if (!decided && ef.ep.WebNetworks?.country != null && maps.Country.TryGetValue(ef.ep.WebNetworks.country.Id, out var wcntInc))
                {
                    if (ef.countryinclude == null) ef.countryinclude = wcntInc;
                    if (wcntInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.Genres != null)
                {
                    foreach (var g in ef.ep.Genres)
                    {
                        if (g.genretext != null && maps.Genre.TryGetValue(g.genretext.Id, out var gInc))
                        {
                            if (ef.genreinclude == null) ef.genreinclude = gInc;
                            if (!decided)
                            {
                                if (gInc) ef.activelyselected = true;
                                else ef.activelyignored = true;
                                decided = true;
                            }
                        }
                    }
                }

                EpFilters.Add(ef);
            }

            return EpFilters.Where(i => !i.activelywatched && !i.activelyselected && !i.activelyignored).ToList();
        }


        public async Task CheckNewSeasonNotifications()
        {
            using var _db = _dbFactory.CreateDbContext();
            // Find S01E01-style episodes (season premiere, episode 1) airing in the last 24 hours
            var recentMin = DateTimeOffset.UtcNow.AddHours(-24);
            var recentMax = DateTimeOffset.UtcNow;

            var newSeasonEps = _db.Episodes
                .Where(a => a.number == 1 && a.season > 1
                    && a.AirDateOffset2 >= recentMin && a.AirDateOffset2 <= recentMax)
                .Include(s => s.show)
                .ToList();

            if (!newSeasonEps.Any()) return;

            newSeasonEps = newSeasonEps.Where(e => e.show != null).ToList();
            if (!newSeasonEps.Any()) return;

            var newSeasonShowIds = newSeasonEps.Select(e => e.show.Id).Distinct().ToHashSet();

            // Find shows selected as wanted
            var wantedShowIds = _db.Shows
                .Where(s => s.Wanted == true && newSeasonShowIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToHashSet();

            foreach (var ep in newSeasonEps)
            {
                if (!wantedShowIds.Contains(ep.show.Id)) continue;
                try
                {
                    await _notifications.SendAsync(
                        $"New Season: {ep.show.name}",
                        $"Season {ep.season} has started airing");
                    _logger.LogInformation("New season notification: {ShowName} S{Season}", ep.show.name, ep.season);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify about {ShowName} S{Season}", ep.show.name, ep.season);
                }
            }
        }

        public HomePageStats HomePageStats()
        {
            using var _db = _dbFactory.CreateDbContext();
            HomePageStats hps = new HomePageStats();

            hps.shows = _db.Shows.Count();
            hps.episodes = _db.Episodes.Count();

            hps.showsNeedingUpdate = _db.Shows.Count(a => a.needsupdate);
            hps.watchedEpisodes = _db.Episodes.Count(e => e.Watched);

            var cutoff24h = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds().ToString();
            hps.recentshows = _db.Shows
                .Where(a => !string.IsNullOrEmpty(a.name) && !a.needsupdate
                    && a.updated != null && a.updated.CompareTo(cutoff24h) >= 0)
                .OrderByDescending(a => a.updated)
                .Include(a => a.Episodes)
                .ToList();


            //List<Show> distinctPage = _db.Shows.Where(a=> a.needsupdate)
            //  .GroupBy(p => p.page)
            //  .Select(g => g.First())
            //  .ToList();

            //hps.backlogpages = distinctPage.Count();


            return hps;
        }

        public async Task<bool> ShowFilter(long id, bool? statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var show = _db.Shows.FirstOrDefault(a => a.Id == id);
                if (show == null) return false;

                var wasUndecided = show.Wanted == null;
                show.Wanted = statewanted;
                await _db.SaveChangesAsync();


                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }


        public async Task<bool> LanguageFilter(long id, bool? statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var entity = _db.Languages.FirstOrDefault(a => a.Id == id);
                if (entity == null) return false;
                entity.Wanted = statewanted;
                _db.Update(entity);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }


        public async Task<bool> TypeFilter(long id, bool? statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var entity = _db.Types.FirstOrDefault(a => a.Id == id);
                if (entity == null) return false;
                entity.Wanted = statewanted;
                _db.Update(entity);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }

        public async Task<bool> NetworkFilter(long id, bool? statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var entity = _db.Networks.FirstOrDefault(a => a.Id == id);
                if (entity == null) return false;
                entity.Wanted = statewanted;
                _db.Update(entity);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }

        public async Task<bool> WebNetworkFilter(long id, bool? statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var entity = _db.WebNetworks.FirstOrDefault(a => a.Id == id);
                if (entity == null) return false;
                entity.Wanted = statewanted;
                _db.Update(entity);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }

        public async Task<bool> GenreFilter(long id, bool? statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var entity = _db.GenreTexts.FirstOrDefault(a => a.Id == id);
                if (entity == null) return false;
                entity.Wanted = statewanted;
                _db.Update(entity);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }

        public async Task<bool> CountryFilter(long id, bool? statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var entity = _db.Countrys.FirstOrDefault(a => a.Id == id);
                if (entity == null) return false;
                entity.Wanted = statewanted;
                _db.Update(entity);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }

        public async Task<bool> SeasonWatchedFilter(long id, long season, bool statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            var s = _db.Shows.Find((int)id);
            if (s == null) return false;

            var episodes = _db.Episodes
                .Where(e => e.show.Id == s.Id && e.season == season)
                .ToList();

            foreach (var ep in episodes)
            {
                ep.Watched = statewanted;
                if (statewanted && ep.GivenUp)
                    ep.GivenUp = false;
            }

            if (episodes.Count > 0)
                await _db.SaveChangesAsync();

            return true;
        }






        public async Task<bool> WatchedFilter(long id, bool statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var ep = _db.Episodes.Find((int)id);
                if (ep == null) return false;

                ep.Watched = statewanted;
                if (statewanted && ep.GivenUp)
                    ep.GivenUp = false;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }

        public List<EpFilter> MissedEpisodes()
        {
            using var _db = _dbFactory.CreateDbContext();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (_db.Database.IsRelational())
                _db.Database.SetCommandTimeout(120);

            // 1. Get wanted show IDs (Wanted = true)
            var wantedShowIds = _db.Shows
                .Where(s => s.Wanted == true)
                .Select(s => s.Id)
                .ToList();
            _logger.LogDebug($"PERF[MissedEpisodes] Wanted shows: {wantedShowIds.Count} in {sw.ElapsedMilliseconds}ms");

            if (wantedShowIds.Count == 0)
                return new List<EpFilter>();

            // 2. Query episodes: wanted shows, aired in the past, not watched, not given up
            var now = DateTimeOffset.UtcNow;
            var eps = _db.Episodes
                .Where(e => wantedShowIds.Contains(e.show.Id)
                    && e.AirDateOffset2 != null
                    && e.AirDateOffset2 < now
                    && !e.Watched
                    && !e.GivenUp)
                .Include(e => e.show)
                .Include(e => e.show.Languages)
                .Include(e => e.show.Types)
                .Include(e => e.show.WebNetworks).ThenInclude(wn => wn.country)
                .Include(e => e.show.Networks).ThenInclude(n => n.country)
                .ToList();
            _logger.LogDebug($"PERF[MissedEpisodes] Missed episodes loaded: {eps.Count} in {sw.ElapsedMilliseconds}ms");

            // 6. Attach genres
            var showIds = eps.Where(e => e.show != null).Select(e => e.show.Id).Distinct().ToHashSet();
            var genresForShows = _db.Genres
                .Include(g => g.genretext)
                .Where(g => g.show != null && showIds.Contains(g.show.Id))
                .ToList();
            var genresByShowId = genresForShows.GroupBy(g => g.show.Id)
                .ToDictionary(g => g.Key, g => (ICollection<Genre>)g.ToList());
            foreach (var ep in eps.Where(e => e.show != null))
            {
                if (genresByShowId.TryGetValue(ep.show.Id, out var genres))
                    ep.show.Genres = genres;
            }

            // Load navigation properties into EF change tracker
            _db.Timezones.ToList();
            _db.GenreTexts.ToList();

            // 7. Build EpFilter list
            var showFilterMap = _db.Shows.Where(s => s.Wanted != null)
                .ToDictionary(s => s.Id, s => s.Wanted.Value);
            var maps = LoadFilterMaps(_db);

            var tvsites = TvSites();

            var result = new List<EpFilter>();
            foreach (var e in eps.Where(a => a.show != null))
            {
                var ef = CreateEpFilter(e, showFilterMap, maps, tvsites);
                result.Add(ef);
            }

            _logger.LogDebug($"PERF[MissedEpisodes] TOTAL: {sw.ElapsedMilliseconds}ms | {result.Count} missed episodes");
            return result.OrderByDescending(a => a.ep.AiringTime)
                .ThenBy(a => a.ep.show.name)
                .ThenBy(a => a.ep.season)
                .ThenBy(a => a.ep.number)
                .ToList();
        }

        public async Task<bool> GivenUpFilter(long id, bool statewanted)
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var ep = _db.Episodes.Find((int)id);
                if (ep == null) return false;

                ep.GivenUp = statewanted;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Filter update failed");
                return false;
            }
        }

        public List<EpFilter> GivenUpEpisodes()
        {
            using var _db = _dbFactory.CreateDbContext();
            var eps = _db.Episodes
                .Where(e => e.GivenUp)
                .Include(e => e.show)
                .Include(e => e.show.Languages)
                .Include(e => e.show.Types)
                .Include(e => e.show.WebNetworks)
                .Include(e => e.show.Networks)
                .ToList();

            // Attach genres
            var showIds = eps.Where(e => e.show != null).Select(e => e.show.Id).Distinct().ToHashSet();
            var genresForShows = _db.Genres
                .Include(g => g.genretext)
                .Where(g => g.show != null && showIds.Contains(g.show.Id))
                .ToList();
            var genresByShowId = genresForShows.GroupBy(g => g.show.Id)
                .ToDictionary(g => g.Key, g => (ICollection<Genre>)g.ToList());
            foreach (var ep in eps.Where(e => e.show != null))
            {
                if (genresByShowId.TryGetValue(ep.show.Id, out var genres))
                    ep.show.Genres = genres;
            }

            // Load navigation properties
            _db.Timezones.ToList();
            _db.GenreTexts.ToList();

            // Load user filter data for filter buttons
            var showFilterMap = _db.Shows.Where(s => s.Wanted != null)
                .ToDictionary(s => s.Id, s => s.Wanted.Value);
            var maps = LoadFilterMaps(_db);

            var tvsites = TvSites();

            var result = new List<EpFilter>();
            foreach (var e in eps.Where(a => a.show != null))
            {
                var ef = CreateEpFilter(e, showFilterMap, maps, tvsites);
                result.Add(ef);
            }

            return result.OrderByDescending(a => a.ep.AiringTime).ThenBy(a => a.ep.show.name).ToList();
        }

        public async Task<bool> SetFolderName(long id, string foldername)
        {
            using var _db = _dbFactory.CreateDbContext();

            var show = _db.Shows.Find((int)id);
            if (show != null)
            {
                show.FolderName = foldername;
                _db.Update(show);
                await _db.SaveChangesAsync();
            }
            return true;
        }


        public Task<List<FileInfo>> Dirlist(string dirName, int daysOldToAllow, string filter = "*.*", int minSizeAllowed = 50000)
        {

            var files = Directory.GetFiles(dirName, filter, SearchOption.AllDirectories).ToList();

            List<FileInfo> filesList = new List<FileInfo>();
            foreach (var f in files)
            {
                var fi = new FileInfo(f);
                if (fi.LastWriteTime >= DateTime.Now.AddDays(0 - daysOldToAllow))
                {
                    if (fi.Length >= minSizeAllowed)
                    {
                        filesList.Add(fi);
                    }
                }
            }
            return Task.FromResult(filesList.OrderByDescending(f => f.LastWriteTime).ToList());

        }


        public Task<List<TouchFile>> ShowDownloaded(int years = 0)
        {
            using var _db = _dbFactory.CreateDbContext();
            var downloads = _db.TouchFiles
                .OrderByDescending(r => r.FileDate)
                .Include(a => a.Episode)
                .Include(a => a.Episode.show);

            return Task.FromResult(downloads.Where(f => f.FileDate.Year == years || years == 0).ToList());
        }

        public async Task<NzBplanetJSON> NZBPlanetSearch(Show show)
        {
            string searchstring = null;
            if (!string.IsNullOrEmpty(show.tvrage))
            {
                searchstring = @$"&rid={show.tvrage}";
            }
            if (!string.IsNullOrEmpty(show.thetvdb))
            {
                searchstring = @$"&tvdbid={show.thetvdb}";
            }
            if (show.showid > 0)
            {
                searchstring = @$"&tvmazeid={show.showid}";
            }

            string url = @$"https://api.nzbplanet.net/api?apikey={_options.NzbPlanetApiKey}&t=tvsearch{searchstring}&o=json&extended=1";
            //string urlxxx = @$"https://nzbgeek.info/geekseek.php?moviesgeekseek=1&c=5000&browseincludewords={searchstring}&o=json&extended=1";
            NzBplanetJSON results;
            try
            {
                results = await url.GetJsonAsync<NzBplanetJSON>();
            }
            catch (FlurlHttpException)
            {
                return null;
            }
            bool moretocome = false;
            int itemsperpass = 100;
            if (results.Channel.Item.Count() > 99)
            {
               moretocome = true;
            }
            itemsperpass       = results.Channel.Item.Count();
            int itemsperpassoffset = itemsperpass;
            while (moretocome && results.Channel.Item.Count() < 500)
            {
                moretocome = false;
                string url2 = $@"{@url}&offset={itemsperpassoffset}";
                NzBplanetJSON passResults;
                try
                {
                    passResults = await url2.GetJsonAsync<NzBplanetJSON>();
                }
                catch (FlurlHttpException)
                {
                    break;
                }
                if (passResults.Channel.Item.Count() > 0)
                {
                    results.Channel.Item.AddRange(passResults.Channel.Item);
                }
                if (passResults.Channel.Item.Count() >= itemsperpass)
                {
                    moretocome=true;
                    itemsperpassoffset += passResults.Channel.Item.Count();
                }
            }
            return results;
        }


        // ===== Feature 2: Statistics =====
        public StatisticsModel GetStatistics()
        {
            using var _db = _dbFactory.CreateDbContext();
            var stats = new StatisticsModel();


            var wantedShows = _db.Shows
                .Where(s => s.Wanted == true)
                .AsNoTracking()
                .ToList();

            var wantedShowIds = wantedShows.Select(s => s.Id).ToHashSet();
            stats.TotalShowsTracked = wantedShowIds.Count;

            stats.ActiveShows = wantedShows.Count(s => s.status != null && s.status != "Ended");
            stats.CompletedShows = wantedShows.Count(s => s.status == "Ended");

            var watchedEps = _db.Episodes
                .Where(e => e.Watched)
                .Include(e => e.show)
                .AsNoTracking()
                .ToList();

            stats.TotalEpisodesWatched = watchedEps.Count;
            stats.TotalWatchTimeMinutes = watchedEps.Sum(e => e.runtimeinmins);

            // Episodes per month
            var byMonth = watchedEps
                .Where(e => e.AirDateOffset2 != null)
                .GroupBy(e => e.AirDateOffset2.Value.ToString("yyyy-MM"))
                .OrderByDescending(g => g.Key)
                .Take(12)
                .ToDictionary(g => g.Key, g => g.Count());
            stats.EpisodesWatchedPerMonth = byMonth;

            // Genre breakdown
            var watchedShowIds = watchedEps
                .Where(e => e.show != null)
                .Select(e => e.show.Id)
                .Distinct()
                .ToHashSet();

            var genres = _db.Genres
                .Include(g => g.genretext)
                .Include(g => g.show)
                .Where(g => g.show != null && watchedShowIds.Contains(g.show.Id) && g.genretext != null)
                .AsNoTracking()
                .ToList()
                .GroupBy(g => g.genretext.genre)
                .ToDictionary(g => g.Key, g => g.Select(x => x.show.Id).Distinct().Count());
            stats.GenreBreakdown = genres;

            // Total episode count per watched show in ONE query.
            // (Previously this was an N+1: a separate COUNT query ran for every watched show.)
            var totalEpsByShow = _db.Episodes
                .Where(e => watchedShowIds.Contains(e.show.Id))
                .GroupBy(e => e.show.Id)
                .Select(g => new { ShowId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ShowId, x => x.Count);

            // Most watched shows
            stats.MostWatchedShows = watchedEps
                .Where(e => e.show != null)
                .GroupBy(e => e.show.Id)
                .Select(g => new ShowWatchStat
                {
                    ShowId = g.Key,
                    ShowName = g.First().show.name ?? "",
                    EpisodesWatched = g.Count(),
                    TotalEpisodes = totalEpsByShow.TryGetValue(g.Key, out var tc) ? tc : 0
                })
                .OrderByDescending(s => s.EpisodesWatched)
                .Take(15)
                .ToList();

            return stats;
        }

        // ===== Feature 3: Search improvements =====
        public async Task<List<TVMazeSearchResult>> SearchTvMaze(string query)
        {
            try
            {
                var results = await $"{_options.TvMazeBaseUrl}/search/shows?q={Uri.EscapeDataString(query)}"
                    .GetJsonAsync<List<TVMazeSearchResult>>();
                return results ?? new List<TVMazeSearchResult>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TVMaze search failed for query: {Query}", query);
                return new List<TVMazeSearchResult>();
            }
        }

        public (List<Show> results, int totalCount) AdvancedSearch(string? name, int? genreId, int? networkId, int? year,
            string? status = null, int? typeId = null, int? webNetworkId = null,
            int? languageId = null, int? countryId = null, string? wanted = null,
            int page = 1, int pageSize = 50)
        {
            using var _db = _dbFactory.CreateDbContext();
            // Build a lean filter query without Includes
            IQueryable<Show> query = _db.Shows.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var terms = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    var t = term;
                    query = query.Where(s => s.name != null && s.name.Contains(t));
                }
            }

            if (networkId.HasValue)
            {
                query = query.Where(s => s.Networks != null && s.Networks.Id == networkId.Value);
            }

            if (webNetworkId.HasValue)
            {
                query = query.Where(s => s.WebNetworks != null && s.WebNetworks.Id == webNetworkId.Value);
            }

            if (languageId.HasValue)
            {
                query = query.Where(s => s.Languages != null && s.Languages.Id == languageId.Value);
            }

            if (countryId.HasValue)
            {
                query = query.Where(s =>
                    (s.Networks != null && s.Networks.country != null && s.Networks.country.Id == countryId.Value) ||
                    (s.WebNetworks != null && s.WebNetworks.country != null && s.WebNetworks.country.Id == countryId.Value));
            }

            if (year.HasValue)
            {
                var yearStr = year.Value.ToString();
                query = query.Where(s => s.premiered != null && s.premiered.Contains(yearStr));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.status == status);
            }

            if (typeId.HasValue)
            {
                query = query.Where(s => s.Types != null && s.Types.Id == typeId.Value);
            }

            if (genreId.HasValue)
            {
                var showIdsWithGenre = _db.Genres
                    .Where(g => g.genretext != null && g.genretext.Id == genreId.Value && g.show != null)
                    .Select(g => g.show.Id)
                    .ToHashSet();

                query = query.Where(s => showIdsWithGenre.Contains(s.Id));
            }

            if (!string.IsNullOrWhiteSpace(wanted))
            {
                switch (wanted)
                {
                    case "wanted":
                        query = query.Where(s => s.Wanted == true);
                        break;
                    case "excluded":
                        query = query.Where(s => s.Wanted == false);
                        break;
                    case "undecided":
                        query = query.Where(s => s.Wanted == null);
                        break;
                }
            }

            // Get total count from lean query
            var totalCount = query.Count();

            // Now get just the page of IDs, then load full details for those IDs only
            var pageIds = query
                .OrderBy(s => s.name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => s.Id)
                .ToList();

            var results = _db.Shows
                .Include(s => s.Networks).ThenInclude(n => n.country)
                .Include(s => s.WebNetworks).ThenInclude(wn => wn.country)
                .Include(s => s.Types)
                .Include(s => s.Languages)
                .Where(s => pageIds.Contains(s.Id))
                .OrderBy(s => s.name)
                .AsNoTracking()
                .ToList();

            return (results, totalCount);
        }

        // ===== Feature 4: Bulk actions =====
        public async Task BulkSetShowFilter(List<long> showIds, bool? state)
        {
            using var _db = _dbFactory.CreateDbContext();


            var shows = _db.Shows.Where(s => showIds.Contains(s.Id)).ToList();
            foreach (var show in shows)
            {
                show.Wanted = state;
            }

            await _db.SaveChangesAsync();
        }

        public async Task CatchUpShow(long showId)
        {
            using var _db = _dbFactory.CreateDbContext();
            var unwatched = _db.Episodes
                .Where(e => e.show.Id == showId && e.AirDateOffset2 < DateTimeOffset.UtcNow && !e.Watched)
                .ToList();

            foreach (var ep in unwatched)
            {
                ep.Watched = true;
                if (ep.GivenUp) ep.GivenUp = false;
            }

            if (unwatched.Any())
                await _db.SaveChangesAsync();
        }

        public async Task GiveUpShow(long showId)
        {
            using var _db = _dbFactory.CreateDbContext();
            var unwatched = _db.Episodes
                .Where(e => e.show.Id == showId && e.AirDateOffset2 < DateTimeOffset.UtcNow && !e.Watched && !e.GivenUp)
                .ToList();

            foreach (var ep in unwatched)
                ep.GivenUp = true;

            if (unwatched.Any())
                await _db.SaveChangesAsync();
        }

        // ===== Feature 6: Download progress =====
        public List<DownloadProgressModel> GetDownloadProgress()
        {
            using var _db = _dbFactory.CreateDbContext();


            var wantedShowIds = _db.Shows
                .Where(s => s.Wanted == true)
                .Select(s => s.Id)
                .ToList();

            var touchFileEpisodeIds = _db.TouchFiles
                .Where(t => t.Episode != null)
                .Select(t => t.Episode.Id)
                .ToHashSet();

            var watchedEpisodeIds = _db.Episodes
                .Where(e => e.Watched)
                .Select(e => e.Id)
                .ToHashSet();

            var downloadedIds = touchFileEpisodeIds.Union(watchedEpisodeIds).ToHashSet();

            var shows = _db.Shows
                .Where(s => wantedShowIds.Contains(s.Id))
                .Include(s => s.Episodes)
                .AsNoTracking()
                .ToList();

            var result = new List<DownloadProgressModel>();
            foreach (var show in shows)
            {
                var airedEps = (show.Episodes ?? new List<Episode>())
                    .Where(e => e.AirDateOffset2 < DateTimeOffset.UtcNow)
                    .ToList();

                if (airedEps.Count == 0) continue;

                var downloaded = airedEps.Count(e => downloadedIds.Contains(e.Id));
                var missing = airedEps.Where(e => !downloadedIds.Contains(e.Id))
                    .OrderBy(e => e.season).ThenBy(e => e.number)
                    .ToList();

                result.Add(new DownloadProgressModel
                {
                    ShowName = show.name ?? "",
                    ShowId = show.Id,
                    TotalAiredEpisodes = airedEps.Count,
                    DownloadedEpisodes = downloaded,
                    MissingEpisodes = missing
                });
            }

            return result.OrderBy(r => r.PercentComplete).ToList();
        }

        // ===== Feature 10: Export/Import =====
        public string ExportUserDataAsJson()
        {
            using var _db = _dbFactory.CreateDbContext();


            var showSelections = _db.Shows
                .Where(s => s.Wanted != null)
                .ToList()
                .Select(s => new ExportShowSelection
                {
                    TvMazeShowId = s.showid,
                    ShowName = s.name ?? "",
                    Include = s.Wanted.Value,
                    FolderName = s.FolderName
                }).ToList();

            var watchedEpisodes = _db.Episodes
                .Where(e => e.Watched)
                .Include(e => e.show)
                .ToList()
                .Where(e => e.show != null)
                .Select(e => new ExportWatchedEpisode
                {
                    TvMazeShowId = e.show.showid,
                    ShowName = e.show.name ?? "",
                    Season = e.season,
                    EpisodeNumber = e.number
                }).ToList();

            var export = new ExportModel
            {
                ExportDate = DateTime.UtcNow,
                ShowSelections = showSelections,
                WatchedEpisodes = watchedEpisodes
            };

            return JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<int> ImportUserDataFromJson(string json)
        {
            using var _db = _dbFactory.CreateDbContext();

            var import = JsonSerializer.Deserialize<ExportModel>(json);
            if (import == null) return 0;

            int imported = 0;

            // Import show selections
            foreach (var sel in import.ShowSelections)
            {
                var show = _db.Shows.FirstOrDefault(s => s.showid == sel.TvMazeShowId);
                if (show == null) continue;

                if (show.Wanted == null)
                {
                    show.Wanted = sel.Include;
                    imported++;
                }

                if (!string.IsNullOrEmpty(sel.FolderName) && string.IsNullOrEmpty(show.FolderName))
                {
                    show.FolderName = sel.FolderName;
                }
            }

            // Import watched episodes
            foreach (var watched in import.WatchedEpisodes)
            {
                var show = _db.Shows.FirstOrDefault(s => s.showid == watched.TvMazeShowId);
                if (show == null) continue;

                var episode = _db.Episodes.FirstOrDefault(e =>
                    e.show.Id == show.Id && e.season == watched.Season && e.number == watched.EpisodeNumber);
                if (episode == null) continue;

                if (!episode.Watched)
                {
                    episode.Watched = true;
                    if (episode.GivenUp) episode.GivenUp = false;
                    imported++;
                }
            }

            await _db.SaveChangesAsync();
            return imported;
        }


        // ===== NEW FEATURES =====

        public async Task SetShowNotes(long showId, string notes)
        {
            using var _db = _dbFactory.CreateDbContext();
            var show = _db.Shows.Find((int)showId);
            if (show != null)
            {
                show.Notes = notes;
                _db.Update(show);
                await _db.SaveChangesAsync();
            }
        }

        public async Task SetShowPriority(long showId, int priority)
        {
            using var _db = _dbFactory.CreateDbContext();
            var show = _db.Shows.Find((int)showId);
            if (show != null)
            {
                show.Priority = priority;
                await _db.SaveChangesAsync();
            }
        }

        public Dictionary<int, (int watched, int total)> GetEpisodeCountsForShows(List<int> showIds)
        {
            using var _db = _dbFactory.CreateDbContext();
            var totalByShow = _db.Episodes
                .Where(e => showIds.Contains(e.show.Id))
                .GroupBy(e => e.show.Id)
                .Select(g => new { ShowId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ShowId, x => x.Count);

            var watchedByShow = _db.Episodes
                .Where(e => e.Watched && showIds.Contains(e.show.Id))
                .GroupBy(e => e.show.Id)
                .Select(g => new { ShowId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.ShowId, x => x.Count);

            var result = new Dictionary<int, (int watched, int total)>();
            foreach (var id in showIds)
            {
                totalByShow.TryGetValue(id, out var total);
                watchedByShow.TryGetValue(id, out var watched);
                result[id] = (watched, total);
            }
            return result;
        }

        public List<EpFilter> TonightsEpisodes()
        {
            return AiringAroundNowForUser(0, 0);
        }

        public List<Show> GetSimilarShows(long showId, int max = 5)
        {
            using var _db = _dbFactory.CreateDbContext();
            var showGenreIds = _db.Genres
                .Where(g => g.show != null && g.show.Id == showId && g.genretext != null)
                .Select(g => g.genretext.Id)
                .ToList();

            if (!showGenreIds.Any()) return new List<Show>();

            // Build all exclusion sets from user filter settings
            // Exclude shows explicitly hidden OR already wanted (already decided on)
            var excludedShowIds = _db.Shows
                .Where(s => s.Wanted != null)
                .Select(s => s.Id).ToHashSet();
            var excludedNetworkIds = _db.Networks.Where(n => n.Wanted == false).Select(n => n.Id).ToHashSet();
            var excludedWebNetworkIds = _db.WebNetworks.Where(n => n.Wanted == false).Select(n => n.Id).ToHashSet();
            var excludedTypeIds = _db.Types.Where(t => t.Wanted == false).Select(t => t.Id).ToHashSet();
            var excludedLanguageIds = _db.Languages.Where(l => l.Wanted == false).Select(l => l.Id).ToHashSet();
            var excludedCountryIds = _db.Countrys.Where(c => c.Wanted == false).Select(c => c.Id).ToHashSet();
            var excludedGenreIds = _db.GenreTexts.Where(g => g.Wanted == false).Select(g => g.Id).ToHashSet();

            var similar = _db.Genres
                .Where(g => g.show != null && g.show.Id != showId
                    && g.genretext != null && showGenreIds.Contains(g.genretext.Id))
                .GroupBy(g => g.show.Id)
                .Select(g => new { ShowId = g.Key, MatchCount = g.Count() })
                .OrderByDescending(x => x.MatchCount)
                .ToList();

            var candidateIds = similar.Select(s => (int)s.ShowId).ToList();
            var candidates = _db.Shows
                .Include(s => s.Networks).ThenInclude(n => n.country)
                .Include(s => s.WebNetworks).ThenInclude(w => w.country)
                .Include(s => s.Types)
                .Include(s => s.Languages)
                .Include(s => s.Genres).ThenInclude(g => g.genretext)
                .Where(s => candidateIds.Contains(s.Id))
                .AsNoTracking()
                .ToList();

            // Apply all user filters - exclude if any filter matches
            var filtered = candidates.Where(s =>
            {
                if (excludedShowIds.Contains(s.Id)) return false;
                if (s.Networks != null && excludedNetworkIds.Contains(s.Networks.Id)) return false;
                if (s.WebNetworks != null && excludedWebNetworkIds.Contains(s.WebNetworks.Id)) return false;
                if (s.Types != null && excludedTypeIds.Contains(s.Types.Id)) return false;
                if (s.Languages != null && excludedLanguageIds.Contains(s.Languages.Id)) return false;
                if (s.Networks?.country != null && excludedCountryIds.Contains(s.Networks.country.Id)) return false;
                if (s.WebNetworks?.country != null && excludedCountryIds.Contains(s.WebNetworks.country.Id)) return false;
                if (s.Genres != null && s.Genres.Any(g => g.genretext != null && excludedGenreIds.Contains(g.genretext.Id))) return false;
                return true;
            });

            // Preserve the original similarity ranking
            var rankedIds = similar.Select(s => (int)s.ShowId).ToList();
            return filtered
                .OrderBy(s => rankedIds.IndexOf(s.Id))
                .Take(max)
                .ToList();
        }

        public List<Show> FindDuplicateShows()
        {
            using var _db = _dbFactory.CreateDbContext();
            var dupeTvMazeIds = _db.Shows
                .GroupBy(s => s.showid)
                .Select(g => new { ShowId = g.Key, Count = g.Count() })
                .Where(x => x.Count > 1)
                .Select(x => x.ShowId)
                .ToList();

            return _db.Shows.Where(s => dupeTvMazeIds.Contains(s.showid))
                .OrderBy(s => s.showid).ThenBy(s => s.Id)
                .ToList();
        }

        public async Task<List<TrendingShowModel>> GetTrendingShows()
        {
            using var _db = _dbFactory.CreateDbContext();
            try
            {
                var schedule = await ($"{_options.TvMazeBaseUrl}/schedule")
                    .GetJsonAsync<List<System.Text.Json.JsonElement>>();

                // Map TVMaze show id -> local DB id in a single query (avoids an N+1
                // FirstOrDefault per schedule item below).
                var localIdByShowId = _db.Shows
                    .Select(s => new { s.showid, s.Id })
                    .ToList()
                    .GroupBy(x => x.showid)
                    .ToDictionary(g => g.Key, g => g.First().Id);
                var localShowIds = localIdByShowId.Keys.ToHashSet();
                var wantedShowIds = _db.Shows
                    .Where(s => s.Wanted == true)
                    .Select(s => s.showid)
                    .ToHashSet();
                var ignoredShowIds = _db.Shows
                    .Where(s => s.Wanted == false)
                    .Select(s => s.showid)
                    .ToHashSet();
                var excludedTypes = _db.Types
                    .Where(t => t.Wanted == false)
                    .Select(t => t.type)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var shows = new Dictionary<long, TrendingShowModel>();
                foreach (var item in schedule)
                {
                    if (!item.TryGetProperty("show", out var showEl)) continue;
                    var id = showEl.GetProperty("id").GetInt64();
                    if (ignoredShowIds.Contains(id)) continue;
                    var showType = showEl.TryGetProperty("type", out var tpCheck) ? tpCheck.GetString() : null;
                    if (showType != null && excludedTypes.Contains(showType)) continue;
                    if (shows.ContainsKey(id)) { shows[id].EpisodeCount++; continue; }

                    shows[id] = new TrendingShowModel
                    {
                        TvMazeId = id,
                        Name = showEl.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Network = showEl.TryGetProperty("network", out var net) && net.ValueKind != System.Text.Json.JsonValueKind.Null
                            ? (net.TryGetProperty("name", out var nn) ? nn.GetString() : null) : null,
                        ImageUrl = showEl.TryGetProperty("image", out var img) && img.ValueKind != System.Text.Json.JsonValueKind.Null
                            ? (img.TryGetProperty("medium", out var m) ? m.GetString() : null) : null,
                        Status = showEl.TryGetProperty("status", out var st) ? st.GetString() : null,
                        Type = showEl.TryGetProperty("type", out var tp) ? tp.GetString() : null,
                        Summary = showEl.TryGetProperty("summary", out var sm) ? sm.GetString() : null,
                        EpisodeCount = 1,
                        AlreadyTracked = wantedShowIds.Contains(id) || localShowIds.Contains(id),
                        LocalShowId = localIdByShowId.TryGetValue(id, out var localId) ? localId : (int?)null
                    };
                }

                return shows.Values
                    .OrderByDescending(s => s.EpisodeCount)
                    .ThenBy(s => s.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch trending shows from TVMaze");
                return new List<TrendingShowModel>();
            }
        }

        public ShowComparisonModel CompareShows(long showId1, long showId2)
        {
            using var _db = _dbFactory.CreateDbContext();
            var model = new ShowComparisonModel();
            model.Show1 = BuildComparisonSide(_db, showId1);
            model.Show2 = BuildComparisonSide(_db, showId2);
            return model;
        }

        private ShowComparisonSide BuildComparisonSide(ShowlistDbContext _db, long showId)
        {
            var show = _db.Shows
                .Include(s => s.Episodes)
                .Include(s => s.Networks)
                .Include(s => s.WebNetworks)
                .Include(s => s.Genres).ThenInclude(g => g.genretext)
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == showId);

            if (show == null) return new ShowComparisonSide();

            var watchedCount = show.Episodes?.Count(e => e.Watched) ?? 0;

            return new ShowComparisonSide
            {
                Show = show,
                TotalEpisodes = show.Episodes?.Count ?? 0,
                AiredEpisodes = show.Episodes?.Count(e => e.AirDateOffset2 < DateTimeOffset.UtcNow) ?? 0,
                WatchedEpisodes = watchedCount,
                Genres = string.Join(", ", show.Genres?.Select(g => g.genretext?.genre).Where(g => g != null) ?? Array.Empty<string>()),
                Network = show.Networks?.name ?? show.WebNetworks?.name,
                Seasons = show.Episodes?.Select(e => e.season ?? 0).Distinct().Count() ?? 0
            };
        }

        public string ExportUserDataAsCsv()
        {
            using var _db = _dbFactory.CreateDbContext();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Type,ShowId,ShowName,Season,Episode,Include,FolderName");

            var shows = _db.Shows.Where(s => s.Wanted != null).ToList();
            foreach (var s in shows)
            {
                sb.AppendLine($"ShowSelection,{s.showid},\"{s.name?.Replace("\"", "\"\"")}\",,,{s.Wanted},{s.FolderName}");
            }

            var watched = _db.Episodes
                .Where(e => e.Watched)
                .Include(e => e.show)
                .ToList()
                .Where(e => e.show != null);
            foreach (var e in watched)
            {
                sb.AppendLine($"Watched,{e.show.showid},\"{e.show.name?.Replace("\"", "\"\"")}\",{e.season},{e.number},,");
            }

            return sb.ToString();
        }

        public StorageDashboardModel GetStorageDashboard()
        {
            using var _db = _dbFactory.CreateDbContext();
            var model = new StorageDashboardModel();

            var tvDirs = _db.TVDirectories
                .Where(d => d.DaysToScan != 0)
                .ToList();

            if (!tvDirs.Any())
                return model;

            // Build folder -> show lookup
            var allShows = _db.Shows.AsNoTracking().ToList();
            var wantedShowIds = _db.Shows
                .Where(s => s.Wanted == true)
                .Select(s => s.Id)
                .ToHashSet();

            var folderToShow = new Dictionary<string, Show>(StringComparer.OrdinalIgnoreCase);
            foreach (var show in allShows)
            {
                var folder = show.FolderName;
                if (string.IsNullOrEmpty(folder))
                    folder = show.DefaultFolderName;
                if (!string.IsNullOrEmpty(folder) && !folderToShow.ContainsKey(folder))
                    folderToShow[folder] = show;
            }

            // Aggregate show folders across all TV directories, merging if the same folder appears in multiple
            var showFolderData = new Dictionary<string, (long size, int fileCount, int seasonCount, string firstDir)>(StringComparer.OrdinalIgnoreCase);

            foreach (var tvdir in tvDirs)
            {
                if (string.IsNullOrWhiteSpace(tvdir.Name) || !Directory.Exists(tvdir.Name.Trim()))
                    continue;

                var basePath = tvdir.Name.Trim();
                string[] dirs;
                try
                {
                    dirs = Directory.GetDirectories(basePath);
                }
                catch { continue; }

                foreach (var dir in dirs)
                {
                    var folderName = Path.GetFileName(dir);
                    long size = 0;
                    int fileCount = 0;
                    int seasonCount = 0;

                    try
                    {
                        var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
                        foreach (var f in files)
                        {
                            var fi = new FileInfo(f);
                            size += fi.Length;
                            fileCount++;
                        }
                        seasonCount = Directory.GetDirectories(dir).Length;
                    }
                    catch { continue; }

                    if (showFolderData.TryGetValue(folderName, out var existing))
                    {
                        showFolderData[folderName] = (existing.size + size, existing.fileCount + fileCount,
                            Math.Max(existing.seasonCount, seasonCount), existing.firstDir);
                    }
                    else
                    {
                        showFolderData[folderName] = (size, fileCount, seasonCount, basePath);
                    }
                }
            }

            model.TotalFolders = showFolderData.Count;

            foreach (var (folderName, data) in showFolderData)
            {
                model.TotalSizeBytes += data.size;

                if (folderToShow.TryGetValue(folderName, out var show))
                {
                    model.MatchedFolders++;
                    model.Shows.Add(new ShowStorageInfo
                    {
                        ShowId = show.Id,
                        ShowName = show.name ?? folderName,
                        FolderName = folderName,
                        SizeBytes = data.size,
                        FileCount = data.fileCount,
                        SeasonCount = data.seasonCount,
                        Status = show.status ?? "",
                        IsWanted = wantedShowIds.Contains(show.Id)
                    });
                }
                else
                {
                    model.UnmatchedFolders++;
                    model.UnmatchedFolderNames.Add(folderName);
                    model.Shows.Add(new ShowStorageInfo
                    {
                        FolderName = folderName,
                        SizeBytes = data.size,
                        FileCount = data.fileCount,
                        SeasonCount = data.seasonCount
                    });
                }
            }

            model.Shows = model.Shows.OrderByDescending(s => s.SizeBytes).ToList();
            model.UnmatchedFolderNames.Sort();
            return model;
        }

        private record ParsedPathLine(string ShowFolder, long? Season, long? Episode);

        private (Dictionary<string, Show> folderLookup, List<ParsedPathLine> parsed, int linesSkipped, HashSet<string> unmatchedFolders) ParseImportPaths(ShowlistDbContext _db, string fileContent)
        {
            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var allShows = _db.Shows.ToList();
            var folderLookup = new Dictionary<string, Show>(StringComparer.OrdinalIgnoreCase);
            var wantedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var show in allShows)
            {
                var folder = show.FolderName;
                if (string.IsNullOrEmpty(folder))
                    folder = show.DefaultFolderName;
                if (string.IsNullOrEmpty(folder)) continue;

                // Track wanted show folders so we can hide them from unmatched list
                if (show.Wanted != null)
                    wantedFolders.Add(folder);

                if (show.ShowStart == DateTime.MinValue || show.ShowStart.Year > 2015)
                    continue;
                if (!folderLookup.ContainsKey(folder))
                    folderLookup[folder] = show;
            }

            var parsed = new List<ParsedPathLine>();
            var unmatchedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int linesSkipped = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var parts = trimmed.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) { linesSkipped++; continue; }

                var fileName = parts[parts.Length - 1];
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                if (ext != ".mkv" && ext != ".avi" && ext != ".mp4" && ext != ".wmv"
                    && ext != ".flv" && ext != ".mov" && ext != ".m4v" && ext != ".ts"
                    && ext != ".mpg" && ext != ".mpeg" && ext != ".webm")
                { linesSkipped++; continue; }

                var epParsed = EpisodeNameParser.ParseFirst(fileName);
                long? seasonNum = epParsed?.season;
                long? episodeNum = epParsed?.episode;

                string showFolder = null;
                for (int i = parts.Length - 2; i >= 0; i--)
                {
                    var part = parts[i];
                    if (Regex.IsMatch(part, @"^(Season|Series|S)\s*\d+$", RegexOptions.IgnoreCase)
                        || part.Equals("Specials", StringComparison.OrdinalIgnoreCase))
                        continue;
                    showFolder = part;
                    break;
                }

                if (string.IsNullOrEmpty(showFolder)) { linesSkipped++; continue; }

                if (!folderLookup.ContainsKey(showFolder))
                {
                    if (!wantedFolders.Contains(showFolder))
                        unmatchedFolders.Add(showFolder);
                    linesSkipped++;
                    continue;
                }

                parsed.Add(new ParsedPathLine(showFolder, seasonNum, episodeNum));
            }

            return (folderLookup, parsed, linesSkipped, unmatchedFolders);
        }

        public ImportPathsPreview PreviewImportWatchedFromPaths(string fileContent)
        {
            using var _db = _dbFactory.CreateDbContext();
            var (folderLookup, parsed, linesSkipped, unmatchedFolders) = ParseImportPaths(_db, fileContent);

            var existingWantedShowIds = _db.Shows.Where(s => s.Wanted != null).Select(s => s.Id).ToHashSet();

            // Group parsed lines by show
            var byShow = parsed
                .Where(p => folderLookup.ContainsKey(p.ShowFolder))
                .GroupBy(p => p.ShowFolder, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var preview = new ImportPathsPreview
            {
                LinesSkipped = linesSkipped,
                UnmatchedFolders = unmatchedFolders.OrderBy(x => x).ToList()
            };

            foreach (var g in byShow)
            {
                var show = folderLookup[g.Key];
                if (existingWantedShowIds.Contains(show.Id)) continue;

                var episodes = g.Where(p => p.Season.HasValue && p.Episode.HasValue).ToList();
                var maxSe = episodes.OrderByDescending(e => e.Season).ThenByDescending(e => e.Episode).FirstOrDefault();

                var range = maxSe != null
                    ? $"Up to S{maxSe.Season:D2}E{maxSe.Episode:D2}"
                    : "";

                preview.MatchedShows.Add(new ImportPathsShowMatch
                {
                    ShowId = show.Id,
                    FolderName = g.Key,
                    ShowName = show.name ?? "",
                    EpisodeCount = episodes.Count,
                    EpisodeRange = range,
                    AlreadyWanted = false
                });
                preview.TotalEpisodes += episodes.Count;
            }

            preview.MatchedShows = preview.MatchedShows.OrderBy(s => s.ShowName).ToList();
            return preview;
        }

        public async Task<(int showsMatched, int episodesMarked)> CommitImportWatchedFromPaths(string fileContent)
        {
            using var _db = _dbFactory.CreateDbContext();
            var (folderLookup, parsed, _, _) = ParseImportPaths(_db, fileContent);

            var existingWantedShowIds = _db.Shows.Where(s => s.Wanted != null).Select(s => s.Id).ToHashSet();
            var existingWatchedEpIds = _db.Episodes.Where(e => e.Watched).Select(e => e.Id).ToHashSet();

            // Group by show and find the max episode per show
            var byShow = parsed
                .Where(p => folderLookup.ContainsKey(p.ShowFolder) && !existingWantedShowIds.Contains(folderLookup[p.ShowFolder].Id))
                .GroupBy(p => p.ShowFolder, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var matchedShowIds = new HashSet<int>();
            int episodesMarked = 0;

            foreach (var g in byShow)
            {
                var show = folderLookup[g.Key];
                show.Wanted = true;
                matchedShowIds.Add(show.Id);

                var withEpisodes = g.Where(p => p.Season.HasValue && p.Episode.HasValue).ToList();
                if (!withEpisodes.Any()) continue;

                // Find the highest season/episode from the file list
                var maxEntry = withEpisodes
                    .OrderByDescending(p => p.Season)
                    .ThenByDescending(p => p.Episode)
                    .First();

                // Mark all episodes up to and including the max as watched
                var episodesToMark = _db.Episodes
                    .Where(e => e.show.Id == show.Id &&
                        (e.season < maxEntry.Season || (e.season == maxEntry.Season && e.number <= maxEntry.Episode)))
                    .ToList();

                foreach (var ep in episodesToMark)
                {
                    if (!existingWatchedEpIds.Contains(ep.Id))
                    {
                        ep.Watched = true;
                        if (ep.GivenUp) ep.GivenUp = false;
                        existingWatchedEpIds.Add(ep.Id);
                        episodesMarked++;
                    }
                }
            }

            await _db.SaveChangesAsync();
            return (matchedShowIds.Count, episodesMarked);
        }

        public List<DuplicateFileEntry> FindDuplicateEpisodeFiles()
        {
            using var _db = _dbFactory.CreateDbContext();
            var dirs = _db.TVDirectories
                .Where(d => d.DaysToScan != 0)
                .ToList();

            var allShows = _db.Shows.Where(s => s.Wanted == true).AsNoTracking().ToList();

            var allFiles = new List<DuplicateFileEntry>();

            foreach (var tvdir in dirs)
            {
                if (string.IsNullOrWhiteSpace(tvdir.Name) || !Directory.Exists(tvdir.Name.Trim()))
                    continue;

                List<FileInfo> files;
                try
                {
                    // Only include video file extensions for dedupe check
                    var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ".mkv", ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".flv", ".webm", ".mpeg", ".mpg", ".ts", ".m2ts"
                    };

                    files = Directory.GetFiles(tvdir.Name.Trim(), tvdir.Filter ?? "*.*", SearchOption.AllDirectories)
                        .Select(f => new FileInfo(f))
                        .Where(fi => fi.Length > 0 && videoExtensions.Contains(fi.Extension))
                        .ToList();
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var fi in files)
                {
                    if (fi.DirectoryName == null || fi.DirectoryName.Length <= 5) continue;

                    var parsed = EpisodeNameParser.ParseFirst(fi.Name);
                    if (parsed == null || parsed.Value.episode == 0 || parsed.Value.season == 0) continue;

                    var dirsplit = fi.DirectoryName.ToLower().Split(Path.DirectorySeparatorChar);
                    var showFolderName = dirsplit.Last();
                    if (dirsplit.Length >= 2 && showFolderName.StartsWith("season "))
                    {
                        showFolderName = dirsplit[dirsplit.Length - 2];
                    }

                    var show = allShows.FirstOrDefault(u =>
                        !string.IsNullOrEmpty(u.FolderName) &&
                        u.FolderName.Trim().Equals(showFolderName, StringComparison.OrdinalIgnoreCase));
                    if (show == null)
                    {
                        show = allShows.FirstOrDefault(u =>
                            !string.IsNullOrEmpty(u.name) &&
                            u.name.Equals(showFolderName, StringComparison.OrdinalIgnoreCase));
                    }

                    var showName = show?.name ?? showFolderName;

                    allFiles.Add(new DuplicateFileEntry
                    {
                        ShowName = showName,
                        ShowFolderName = showFolderName,
                        Season = parsed.Value.season,
                        Episode = parsed.Value.episode,
                        Directory = fi.DirectoryName,
                        FileName = fi.Name,
                        FileSize = fi.Length,
                        FileDate = fi.LastWriteTime
                    });
                }
            }

            // Group by show/season/episode and keep only groups with more than one file
            var duplicates = allFiles
                .GroupBy(f => f.GroupKey, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .OrderBy(f => f.ShowName)
                .ThenBy(f => f.Season)
                .ThenBy(f => f.Episode)
                .ThenByDescending(f => f.FileSize)
                .ToList();

            return duplicates;
        }

        public bool DeleteFile(string filePath)
        {
            using var _db = _dbFactory.CreateDbContext();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            // Only ever delete files that live *inside* a configured TV directory.
            // This blocks path traversal / arbitrary-file-deletion via the dedupe UI.
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeleteFile: invalid path {FilePath}", filePath);
                return false;
            }

            var allowedRoots = _db.TVDirectories
                .Where(d => !string.IsNullOrWhiteSpace(d.Name))
                .Select(d => d.Name)
                .ToList()
                .Select(n =>
                {
                    try { return Path.GetFullPath(n.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
                    catch { return null; }
                })
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            bool insideAllowedRoot = allowedRoots.Any(root =>
                fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

            if (!insideAllowedRoot)
            {
                _logger.LogWarning("DeleteFile: refused to delete {FilePath} - not within any configured TV directory", fullPath);
                return false;
            }

            try
            {
                File.Delete(fullPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeleteFile: failed to delete {FilePath}", fullPath);
                return false;
            }
        }

        public async Task<NzbSiteCrawlSummary> CrawlNzbSitesForShow(long showId)
        {
            using var _db = _dbFactory.CreateDbContext();
            var summary = new NzbSiteCrawlSummary();

            var show = _db.Shows
                .Include(s => s.Episodes)
                .FirstOrDefault(s => s.Id == showId);

            if (show == null)
            {
                summary.Errors.Add("Show not found");
                return summary;
            }

            // Get unwatched episodes - include those airing in the next 2 days
            var twoDaysFromNow = DateTimeOffset.UtcNow.AddDays(2);
            var unwatchedEpisodes = (show.Episodes ?? new List<Episode>())
                .Where(e => !e.Watched && !e.GivenUp && e.AirDateOffset2 < twoDaysFromNow)
                .OrderByDescending(e => e.season)
                .ThenByDescending(e => e.number)
                .ToList();

            if (!unwatchedEpisodes.Any())
            {
                summary.Errors.Add("No unwatched episodes to search for");
                return summary;
            }

            // Get active TV sites
            var sites = _db.TVSites.Where(s => s.Active).OrderBy(s => s.Order).ToList();
            if (!sites.Any())
            {
                summary.Errors.Add("No active search sites configured");
                return summary;
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            foreach (var site in sites)
            {
                if (string.IsNullOrEmpty(site.URLTemplate)) continue;

                var crawledInfo = new CrawledSiteInfo
                {
                    SiteName = site.Name ?? "Unknown",
                    Url = ""
                };

                try
                {
                    // Check if site has API key configured - use Newznab API if available
                    if (!string.IsNullOrEmpty(site.ApiKey))
                    {
                        var apiResults = await CrawlWithNewznabApi(httpClient, site, show, unwatchedEpisodes, crawledInfo, summary);
                        summary.Results.AddRange(apiResults);
                        summary.CrawledUrls.Add(crawledInfo);
                        if (crawledInfo.Success) summary.SitesCrawled++;
                        continue;
                    }

                    // Fallback to HTML scraping (requires user to be logged in - won't work server-side)
                    var searchUrl = site.URLTemplate
                        .Replace("{URLSearchTerm}", Uri.EscapeDataString(show.URLSearchTerm))
                        .Replace("{URLSearchTermNameOnly}", Uri.EscapeDataString(show.URLSearchTermNameOnly))
                        .Replace("{URLSearchTermGeekSeek}", Uri.EscapeDataString(show.URLSearchTermGeekSeek));

                    crawledInfo.Url = searchUrl;

                    var response = await httpClient.GetAsync(searchUrl);
                    crawledInfo.HttpStatus = (int)response.StatusCode;

                    if (!response.IsSuccessStatusCode)
                    {
                        crawledInfo.Success = false;
                        crawledInfo.ErrorMessage = $"HTTP {(int)response.StatusCode}";
                        summary.Errors.Add($"{site.Name}: HTTP {(int)response.StatusCode}");
                        summary.CrawledUrls.Add(crawledInfo);
                        continue;
                    }

                    var html = await response.Content.ReadAsStringAsync();
                    crawledInfo.Success = true;
                    summary.SitesCrawled++;

                    // Parse HTML for NZB links - this is a basic implementation
                    // that looks for common patterns in NZB site HTML
                    var (results, debugInfo) = ParseNzbSiteHtml(html, site.Name ?? "Unknown", searchUrl, unwatchedEpisodes);
                    summary.Results.AddRange(results);
                    summary.DebugInfo.AddRange(debugInfo);
                }
                catch (TaskCanceledException)
                {
                    crawledInfo.Success = false;
                    crawledInfo.ErrorMessage = "Timeout";
                    summary.Errors.Add($"{site.Name}: Timeout");
                }
                catch (Exception ex)
                {
                    crawledInfo.Success = false;
                    crawledInfo.ErrorMessage = ex.Message;
                    summary.Errors.Add($"{site.Name}: {ex.Message}");
                }

                summary.CrawledUrls.Add(crawledInfo);
            }

            // Filter results to only unwatched episodes and deduplicate
            var unwatchedCodes = unwatchedEpisodes.Select(e => e.EpNumberFormatted).ToHashSet();
            summary.Results = summary.Results
                .Where(r => unwatchedCodes.Any(code => r.EpisodeCode.Contains(code, StringComparison.OrdinalIgnoreCase)
                    || r.Title.Contains(code, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(r => new { r.SiteName, r.Title })
                .Select(g => g.First())
                .OrderByDescending(r => r.EpisodeCode)
                .ThenBy(r => r.SiteName)
                .ToList();

            summary.TotalResults = summary.Results.Count;
            return summary;
        }

        /// <summary>
        /// Crawl an NZB site using the Newznab API (requires API key).
        /// This is the preferred method as it doesn't require browser authentication.
        /// </summary>
        private async Task<List<NzbSiteCrawlResult>> CrawlWithNewznabApi(
            HttpClient httpClient,
            TVSite site,
            Show show,
            List<Episode> unwatchedEpisodes,
            CrawledSiteInfo crawledInfo,
            NzbSiteCrawlSummary summary)
        {
            var results = new List<NzbSiteCrawlResult>();

            // Determine API base URL
            var apiBase = site.ApiBaseUrl;
            if (string.IsNullOrEmpty(apiBase))
            {
                // Try to derive from URLTemplate
                if (!string.IsNullOrEmpty(site.URLTemplate))
                {
                    var uri = new Uri(site.URLTemplate.Split('?')[0].Split('{')[0]);
                    apiBase = $"{uri.Scheme}://{uri.Host}";
                    // Common API endpoints
                    if (uri.Host.Contains("nzbgeek", StringComparison.OrdinalIgnoreCase))
                        apiBase = "https://api.nzbgeek.info";
                }
            }

            if (string.IsNullOrEmpty(apiBase))
            {
                summary.DebugInfo.Add($"[{site.Name}] API: No API base URL configured");
                crawledInfo.Success = false;
                crawledInfo.ErrorMessage = "No API base URL";
                return results;
            }

            // Build API URL for TV search
            // Newznab API: /api?t=tvsearch&apikey=KEY&q=ShowName or &tvdbid=ID
            var showName = Uri.EscapeDataString(show.name ?? "");
            var apiUrl = $"{apiBase}/api?t=tvsearch&apikey={site.ApiKey}&q={showName}&cat=5000";

            crawledInfo.Url = apiUrl.Replace(site.ApiKey!, "[HIDDEN]"); // Don't expose API key in UI
            summary.DebugInfo.Add($"[{site.Name}] API: Using Newznab API");

            try
            {
                var response = await httpClient.GetAsync(apiUrl);
                crawledInfo.HttpStatus = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    crawledInfo.Success = false;
                    crawledInfo.ErrorMessage = $"HTTP {(int)response.StatusCode}";
                    summary.DebugInfo.Add($"[{site.Name}] API: HTTP {(int)response.StatusCode}");
                    return results;
                }

                var xml = await response.Content.ReadAsStringAsync();
                crawledInfo.Success = true;

                // Parse Newznab XML response
                results = ParseNewznabResponse(xml, site.Name ?? "Unknown", apiUrl, unwatchedEpisodes, summary);
                summary.DebugInfo.Add($"[{site.Name}] API: Found {results.Count} results");
            }
            catch (Exception ex)
            {
                crawledInfo.Success = false;
                crawledInfo.ErrorMessage = ex.Message;
                summary.DebugInfo.Add($"[{site.Name}] API Error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Redacts the first match of <paramref name="keyPattern"/> in <paramref name="url"/>
        /// (e.g. an "apikey=..." or "r=..." query param) with <paramref name="replacement"/>.
        /// Returns the URL unchanged if the pattern isn't found, since String.Replace throws
        /// ArgumentException when given an empty oldValue (Regex.Match(...).Value when there's
        /// no match) rather than being a no-op.
        /// </summary>
        private static string RedactUrlParam(string url, string keyPattern, string replacement)
        {
            var match = Regex.Match(url, keyPattern);
            return match.Success ? url.Replace(match.Value, replacement) : url;
        }

        /// <summary>
        /// Parse Newznab API XML response.
        /// </summary>
        internal List<NzbSiteCrawlResult> ParseNewznabResponse(
            string xml,
            string siteName,
            string searchUrl,
            List<Episode> unwatchedEpisodes,
            NzbSiteCrawlSummary summary)
        {
            var results = new List<NzbSiteCrawlResult>();

            // Build unwatched episode codes
            var unwatchedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ep in unwatchedEpisodes)
            {
                unwatchedCodes.Add(ep.EpNumberFormatted);
                unwatchedCodes.Add($"S{ep.season}E{ep.number}");
                unwatchedCodes.Add($"{ep.season}x{ep.number:D2}");
            }

            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                var ns = doc.Root?.GetDefaultNamespace() ?? System.Xml.Linq.XNamespace.None;

                // Check for error response
                var error = doc.Descendants("error").FirstOrDefault();
                if (error != null)
                {
                    var errorCode = error.Attribute("code")?.Value;
                    var errorDesc = error.Attribute("description")?.Value;
                    summary.DebugInfo.Add($"[{siteName}] API Error: {errorCode} - {errorDesc}");
                    return results;
                }

                // Parse items
                var items = doc.Descendants("item");
                summary.DebugInfo.Add($"[{siteName}] API: Parsing {items.Count()} items from XML");

                foreach (var item in items)
                {
                    var title = item.Element("title")?.Value ?? "";
                    var link = item.Element("link")?.Value ?? "";
                    var pubDate = item.Element("pubDate")?.Value;
                    var size = "";

                    // Get size from newznab attributes
                    var attrs = item.Elements().Where(e => e.Name.LocalName == "attr");
                    foreach (var attr in attrs)
                    {
                        var name = attr.Attribute("name")?.Value;
                        var value = attr.Attribute("value")?.Value;
                        if (name == "size" && !string.IsNullOrEmpty(value) && long.TryParse(value, out var bytes))
                        {
                            size = bytes > 1_000_000_000 ? $"{bytes / 1_000_000_000.0:F2} GB" : $"{bytes / 1_000_000.0:F1} MB";
                        }
                    }

                    // Extract episode code from title
                    string? episodeCode = null;
                    var epMatch = Regex.Match(title, @"S(\d{1,2})E(\d{1,2})", RegexOptions.IgnoreCase);
                    if (epMatch.Success)
                    {
                        var season = int.Parse(epMatch.Groups[1].Value);
                        var episode = int.Parse(epMatch.Groups[2].Value);
                        episodeCode = $"S{season:D2}E{episode:D2}";
                    }
                    else
                    {
                        var altMatch = Regex.Match(title, @"(\d{1,2})x(\d{2})", RegexOptions.IgnoreCase);
                        if (altMatch.Success)
                        {
                            var season = int.Parse(altMatch.Groups[1].Value);
                            var episode = int.Parse(altMatch.Groups[2].Value);
                            episodeCode = $"S{season:D2}E{episode:D2}";
                        }
                    }

                    if (string.IsNullOrEmpty(episodeCode)) continue;
                    if (!unwatchedCodes.Contains(episodeCode)) continue;

                    results.Add(new NzbSiteCrawlResult
                    {
                        SiteName = siteName,
                        Title = title.Length > 150 ? title.Substring(0, 150) + "..." : title,
                        EpisodeCode = episodeCode,
                        DownloadUrl = link,
                        Size = size,
                        PostDate = DateTime.TryParse(pubDate, out var dt) ? dt : null,
                        SearchUrl = RedactUrlParam(searchUrl, @"apikey=[^&]+", "apikey=[HIDDEN]")
                    });
                }
            }
            catch (Exception ex)
            {
                summary.DebugInfo.Add($"[{siteName}] API XML Parse Error: {ex.Message}");
            }

            return results;
        }

        internal (List<NzbSiteCrawlResult> results, List<string> debugInfo) ParseNzbSiteHtml(string html, string siteName, string searchUrl, List<Episode> unwatchedEpisodes)
        {
            var results = new List<NzbSiteCrawlResult>();
            var debugInfo = new List<string>();

            debugInfo.Add($"[{siteName}] HTML length: {html.Length} chars");
            debugInfo.Add($"[{siteName}] WARNING: No API key configured - HTML scraping requires login (use API key instead)");

            // Build episode code patterns - match S01E01, S1E1, 1x01, etc.
            var episodePatterns = new[]
            {
                @"S(\d{1,2})E(\d{1,2})",      // S01E01, S1E1
                @"(\d{1,2})x(\d{2})",          // 1x01, 01x01
                @"Season\s*(\d{1,2})\s*Episode\s*(\d{1,2})", // Season 1 Episode 1
            };

            // Build a set of unwatched episode codes in multiple formats for matching
            var unwatchedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ep in unwatchedEpisodes)
            {
                unwatchedCodes.Add(ep.EpNumberFormatted); // S01E01
                unwatchedCodes.Add($"S{ep.season}E{ep.number}"); // S1E1 without padding
                unwatchedCodes.Add($"{ep.season}x{ep.number:D2}"); // 1x01
                unwatchedCodes.Add($"{ep.season:D2}x{ep.number:D2}"); // 01x01
            }

            debugInfo.Add($"[{siteName}] Looking for episodes: {string.Join(", ", unwatchedCodes.Take(10))}{(unwatchedCodes.Count > 10 ? "..." : "")}");

            // Find ALL realistic episode codes present in the HTML (for debugging)
            // Filter out unrealistic ones (season > 30 or episode > 50) which are likely false positives from Base64 data
            var foundEpCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pattern in episodePatterns)
            {
                var allMatches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
                foreach (Match m in allMatches)
                {
                    var s = int.Parse(m.Groups[1].Value);
                    var e = int.Parse(m.Groups[2].Value);
                    // Filter out unrealistic episode codes (likely from Base64/encoded data)
                    if (s <= 30 && e <= 50)
                    {
                        foundEpCodes.Add($"S{s:D2}E{e:D2}");
                    }
                }
            }
            debugInfo.Add($"[{siteName}] Realistic episode codes in HTML: {(foundEpCodes.Any() ? string.Join(", ", foundEpCodes.Take(15)) : "NONE")}");

            // Check for common patterns in HTML
            var hasTable = html.Contains("<table", StringComparison.OrdinalIgnoreCase);
            var hasTr = html.Contains("<tr", StringComparison.OrdinalIgnoreCase);
            var hasDiv = html.Contains("<div", StringComparison.OrdinalIgnoreCase);
            debugInfo.Add($"[{siteName}] HTML contains: table={hasTable}, tr={hasTr}, div={hasDiv}");

            // Show first few anchor tags for debugging
            var sampleAnchors = Regex.Matches(html, @"<a\s+[^>]*href\s*=\s*[""']([^""']{0,100})[""'][^>]*>([^<]{0,100})</a>", RegexOptions.IgnoreCase);
            var anchorSamples = sampleAnchors.Cast<Match>().Take(5).Select(m => $"[{m.Groups[2].Value.Trim()}]({m.Groups[1].Value})");
            if (anchorSamples.Any())
            {
                debugInfo.Add($"[{siteName}] Sample anchors: {string.Join(" | ", anchorSamples)}");
            }

            // Show a sample of readable HTML (skip encoded/binary content)
            var readableHtml = Regex.Replace(html, @"[A-Za-z0-9+/=]{50,}", "[BASE64]"); // Replace long base64 strings
            var bodyMatch = Regex.Match(readableHtml, @"<body[^>]*>([\s\S]{0,5000})", RegexOptions.IgnoreCase);
            if (bodyMatch.Success)
            {
                var sample = bodyMatch.Groups[1].Value;
                sample = Regex.Replace(sample, @"<script[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
                sample = Regex.Replace(sample, @"<style[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
                sample = Regex.Replace(sample, @"<[^>]+>", " ");
                sample = System.Net.WebUtility.HtmlDecode(sample);
                sample = Regex.Replace(sample, @"\s+", " ").Trim();
                if (sample.Length > 500) sample = sample.Substring(0, 500);
                debugInfo.Add($"[{siteName}] Page text sample: {sample}...");
            }

            // Strategy 1: Find anchors containing episode codes - simplest approach
            // Look for <a> tags where the text OR href contains an episode code
            var anchorPattern = @"<a\s+[^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>(.*?)</a>";
            var allAnchors = Regex.Matches(html, anchorPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            debugInfo.Add($"[{siteName}] Total anchor tags found: {allAnchors.Count}");

            int anchorsWithEpCode = 0;
            foreach (Match anchor in allAnchors)
            {
                var href = anchor.Groups[1].Value;
                var text = System.Net.WebUtility.HtmlDecode(anchor.Groups[2].Value);
                text = Regex.Replace(text, @"<[^>]+>", " ").Trim();
                var combined = text + " " + href;

                string episodeCode = null;
                foreach (var pattern in episodePatterns)
                {
                    var epMatch = Regex.Match(combined, pattern, RegexOptions.IgnoreCase);
                    if (epMatch.Success)
                    {
                        var season = int.Parse(epMatch.Groups[1].Value);
                        var episode = int.Parse(epMatch.Groups[2].Value);
                        // Filter out unrealistic episode codes
                        if (season <= 30 && episode <= 50)
                        {
                            episodeCode = $"S{season:D2}E{episode:D2}";
                        }
                        break;
                    }
                }

                if (episodeCode == null) continue;
                anchorsWithEpCode++;

                if (!unwatchedCodes.Contains(episodeCode)) continue;

                // Found a matching anchor! Now find download URL nearby
                var contextStart = Math.Max(0, anchor.Index - 1000);
                var contextEnd = Math.Min(html.Length, anchor.Index + anchor.Length + 2000);
                var context = html.Substring(contextStart, contextEnd - contextStart);

                // Look for download URLs in the context
                var downloadUrl = "";
                var dlPatterns = new[]
                {
                    @"href\s*=\s*[""'](https?://[^""']*(?:get|download|cdn|api)[^""']*)[""']",
                    @"href\s*=\s*[""'](https?://[^""']*\.nzb[^""']*)[""']",
                    @"href\s*=\s*[""'](https?://api\.[^""']+)[""']",
                };
                foreach (var dlPattern in dlPatterns)
                {
                    var dlMatch = Regex.Match(context, dlPattern, RegexOptions.IgnoreCase);
                    if (dlMatch.Success)
                    {
                        downloadUrl = dlMatch.Groups[1].Value;
                        break;
                    }
                }

                // Extract size from context
                var sizeMatch = Regex.Match(context, @"(\d+(?:\.\d+)?\s*(?:GB|MB|GiB|MiB))", RegexOptions.IgnoreCase);
                var size = sizeMatch.Success ? sizeMatch.Value : "";

                var title = text.Length > 5 ? text : episodeCode;

                // Check for duplicate
                if (results.Any(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && r.SiteName == siteName))
                    continue;

                results.Add(new NzbSiteCrawlResult
                {
                    SiteName = siteName,
                    Title = title.Length > 150 ? title.Substring(0, 150) + "..." : title,
                    EpisodeCode = episodeCode,
                    DownloadUrl = downloadUrl,
                    Size = size,
                    SearchUrl = searchUrl
                });
            }

            debugInfo.Add($"[{siteName}] Anchors with episode codes: {anchorsWithEpCode}, Results found: {results.Count}");

            // If anchor strategy worked, return
            if (results.Any())
            {
                debugInfo.Add($"[{siteName}] Final result count: {results.Count}");
                return (results, debugInfo);
            }

            // Strategy 2: Row-based approach (table rows, divs, list items)
            // Pattern to find row-like containers: <tr>...</tr> or <div class="...row...">...</div>
            var rowPatterns = new[]
            {
                @"<tr[^>]*>([\s\S]*?)</tr>",                                    // Table rows ([\s\S] matches newlines too)
                @"<div[^>]*class=""[^""]*(?:row|item|result|release)[^""]*""[^>]*>([\s\S]*?)</div>(?=\s*<div|</)", // Div rows
                @"<li[^>]*>([\s\S]*?)</li>",                                    // List items
            };

            foreach (var rowPattern in rowPatterns)
            {
                var rowMatches = Regex.Matches(html, rowPattern, RegexOptions.IgnoreCase);
                debugInfo.Add($"[{siteName}] Row pattern found {rowMatches.Count} matches");

                int rowsWithEpCode = 0;
                int rowsWithUrl = 0;
                int rowsWithBoth = 0;

                foreach (Match rowMatch in rowMatches)
                {
                    var rowHtml = rowMatch.Groups[1].Value;

                    // REQUIREMENT 1: Row must contain an episode code
                    string episodeCode = null;
                    foreach (var pattern in episodePatterns)
                    {
                        var epMatch = Regex.Match(rowHtml, pattern, RegexOptions.IgnoreCase);
                        if (epMatch.Success)
                        {
                            var season = int.Parse(epMatch.Groups[1].Value);
                            var episode = int.Parse(epMatch.Groups[2].Value);
                            episodeCode = $"S{season:D2}E{episode:D2}";
                            break;
                        }
                    }

                    if (episodeCode != null) rowsWithEpCode++;

                    // REQUIREMENT 2: Row must contain at least one URL (href with http)
                    var urlPattern = @"href\s*=\s*[""'](https?://[^""']+)[""']";
                    var urlMatches = Regex.Matches(rowHtml, urlPattern, RegexOptions.IgnoreCase);

                    if (urlMatches.Count > 0) rowsWithUrl++;

                    if (episodeCode == null || !unwatchedCodes.Contains(episodeCode)) continue;
                    if (urlMatches.Count == 0) continue;

                    rowsWithBoth++;

                    // Extract title - find the longest text containing the episode code
                    string title = null;

                    // First try: Look for anchor tag text containing episode code
                    var rowAnchorPattern = @"<a[^>]*>(.*?)</a>";
                    var rowAnchorMatches = Regex.Matches(rowHtml, rowAnchorPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    foreach (Match anchorMatch in rowAnchorMatches)
                    {
                        var anchorText = System.Net.WebUtility.HtmlDecode(anchorMatch.Groups[1].Value);
                        anchorText = Regex.Replace(anchorText, @"<[^>]+>", " ").Trim();
                        anchorText = Regex.Replace(anchorText, @"\s+", " ");

                        // Check if this anchor contains the episode code
                        foreach (var pattern in episodePatterns)
                        {
                            if (Regex.IsMatch(anchorText, pattern, RegexOptions.IgnoreCase))
                            {
                                if (title == null || anchorText.Length > title.Length)
                                {
                                    title = anchorText;
                                }
                                break;
                            }
                        }
                    }

                    // Fallback: Extract plain text containing episode code
                    if (string.IsNullOrWhiteSpace(title) || title.Length < 10)
                    {
                        var textOnly = Regex.Replace(rowHtml, @"<[^>]+>", " ");
                        textOnly = Regex.Replace(textOnly, @"\s+", " ").Trim();
                        var codeMatch = Regex.Match(textOnly, @"(\S.{0,80}(?:S\d{1,2}E\d{1,2}|\d{1,2}x\d{2}).{0,40})", RegexOptions.IgnoreCase);
                        if (codeMatch.Success)
                        {
                            title = codeMatch.Value.Trim();
                        }
                        else
                        {
                            title = episodeCode;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(title) || title.Length < 3) continue;

                    // Extract download URL - prioritize URLs containing download/get/nzb/api keywords
                    var downloadUrl = "";
                    var downloadKeywords = new[] { "get", "download", "nzb", "api", "cdn" };
                    foreach (Match urlMatch in urlMatches)
                    {
                        var url = urlMatch.Groups[1].Value;
                        if (downloadKeywords.Any(k => url.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        {
                            downloadUrl = url;
                            break;
                        }
                    }

                    // If no download-specific URL found, use the first URL as fallback
                    if (string.IsNullOrEmpty(downloadUrl) && urlMatches.Count > 0)
                    {
                        downloadUrl = urlMatches[0].Groups[1].Value;
                    }

                    // Extract size
                    var sizeMatch = Regex.Match(rowHtml, @"(\d+(?:\.\d+)?\s*(?:GB|MB|GiB|MiB))", RegexOptions.IgnoreCase);
                    var size = sizeMatch.Success ? sizeMatch.Value : "";

                    // Check for duplicate
                    if (results.Any(r => r.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && r.SiteName == siteName))
                        continue;

                    results.Add(new NzbSiteCrawlResult
                    {
                        SiteName = siteName,
                        Title = title.Length > 150 ? title.Substring(0, 150) + "..." : title,
                        EpisodeCode = episodeCode,
                        DownloadUrl = downloadUrl,
                        Size = size,
                        SearchUrl = searchUrl
                    });
                }

                debugInfo.Add($"[{siteName}] Row strategy - rows with ep: {rowsWithEpCode}, with URLs: {rowsWithUrl}, matching unwatched: {rowsWithBoth}");

                // If we found results with this row pattern, don't try others
                if (results.Any()) break;
            }

            debugInfo.Add($"[{siteName}] Final result count: {results.Count}");
            return (results, debugInfo);
        }

        /// <summary>
        /// Crawl NZB sites via RSS feeds. Only processes sites with RssApiKey configured.
        /// Sites without RssApiKey are completely ignored.
        /// </summary>
        public async Task<NzbSiteCrawlSummary> CrawlNzbRssFeedsForShow(long showId)
        {
            using var _db = _dbFactory.CreateDbContext();
            var summary = new NzbSiteCrawlSummary();

            var show = _db.Shows
                .Include(s => s.Episodes)
                .FirstOrDefault(s => s.Id == showId);

            if (show == null)
            {
                summary.Errors.Add("Show not found");
                return summary;
            }

            // Get unwatched episodes - include those airing in the next 2 days
            var twoDaysFromNow = DateTimeOffset.UtcNow.AddDays(2);
            var unwatchedEpisodes = (show.Episodes ?? new List<Episode>())
                .Where(e => !e.Watched && !e.GivenUp && e.AirDateOffset2 < twoDaysFromNow)
                .OrderByDescending(e => e.season)
                .ThenByDescending(e => e.number)
                .ToList();

            if (!unwatchedEpisodes.Any())
            {
                summary.Errors.Add("No unwatched episodes to search for");
                return summary;
            }

            // Get active TV sites that have RSS API key configured
            // Sites without RssApiKey are completely skipped
            var sites = _db.TVSites
                .Where(s => s.Active && !string.IsNullOrEmpty(s.RssApiKey))
                .OrderBy(s => s.Order)
                .ToList();

            if (!sites.Any())
            {
                summary.Errors.Add("No sites configured with RSS API keys");
                return summary;
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            foreach (var site in sites)
            {
                var crawledInfo = new CrawledSiteInfo
                {
                    SiteName = site.Name ?? "Unknown",
                    Url = ""
                };

                try
                {
                    var rssResults = await CrawlWithRssFeed(httpClient, site, show, unwatchedEpisodes, crawledInfo, summary);
                    summary.Results.AddRange(rssResults);
                    summary.CrawledUrls.Add(crawledInfo);
                    if (crawledInfo.Success) summary.SitesCrawled++;
                }
                catch (TaskCanceledException)
                {
                    crawledInfo.Success = false;
                    crawledInfo.ErrorMessage = "Timeout";
                    summary.Errors.Add($"{site.Name}: Timeout");
                    summary.CrawledUrls.Add(crawledInfo);
                }
                catch (Exception ex)
                {
                    crawledInfo.Success = false;
                    crawledInfo.ErrorMessage = ex.Message;
                    summary.Errors.Add($"{site.Name}: {ex.Message}");
                    summary.CrawledUrls.Add(crawledInfo);
                }
            }

            // Filter results to only unwatched episodes and deduplicate
            var unwatchedCodes = unwatchedEpisodes.Select(e => e.EpNumberFormatted).ToHashSet();
            summary.Results = summary.Results
                .Where(r => unwatchedCodes.Any(code => r.EpisodeCode.Contains(code, StringComparison.OrdinalIgnoreCase)
                    || r.Title.Contains(code, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(r => new { r.SiteName, r.Title })
                .Select(g => g.First())
                .OrderByDescending(r => r.EpisodeCode)
                .ThenBy(r => r.SiteName)
                .ToList();

            summary.TotalResults = summary.Results.Count;
            return summary;
        }

        /// <summary>
        /// Crawl an NZB site using its RSS feed (requires RSS API key).
        /// Most Newznab-compatible sites support RSS feeds at /rss or /api?t=search&dl=1
        /// </summary>
        private async Task<List<NzbSiteCrawlResult>> CrawlWithRssFeed(
            HttpClient httpClient,
            TVSite site,
            Show show,
            List<Episode> unwatchedEpisodes,
            CrawledSiteInfo crawledInfo,
            NzbSiteCrawlSummary summary)
        {
            var results = new List<NzbSiteCrawlResult>();

            // Determine RSS base URL
            var rssBase = site.RssBaseUrl;
            if (string.IsNullOrEmpty(rssBase))
            {
                // Try to derive from ApiBaseUrl or URLTemplate
                if (!string.IsNullOrEmpty(site.ApiBaseUrl))
                {
                    rssBase = site.ApiBaseUrl;
                }
                else if (!string.IsNullOrEmpty(site.URLTemplate))
                {
                    var uri = new Uri(site.URLTemplate.Split('?')[0].Split('{')[0]);
                    rssBase = $"{uri.Scheme}://{uri.Host}";
                    // Common API endpoints for specific sites
                    if (uri.Host.Contains("nzbgeek", StringComparison.OrdinalIgnoreCase))
                        rssBase = "https://api.nzbgeek.info";
                }
            }

            if (string.IsNullOrEmpty(rssBase))
            {
                summary.DebugInfo.Add($"[{site.Name}] RSS: No RSS base URL configured or derivable");
                crawledInfo.Success = false;
                crawledInfo.ErrorMessage = "No RSS base URL";
                return results;
            }

            // Build RSS feed URL for TV search
            // Newznab RSS feed: /rss?t=5000&dl=1&i=ID&r=APIKEY or /api?t=search&apikey=KEY&q=ShowName&cat=5000
            var showName = Uri.EscapeDataString(show.name ?? "");
            var rssUrl = $"{rssBase}/api?t=tvsearch&r={site.RssApiKey}&q={showName}&cat=5000&dl=1";

            crawledInfo.Url = rssUrl.Replace(site.RssApiKey!, "[HIDDEN]"); // Don't expose API key in UI
            summary.DebugInfo.Add($"[{site.Name}] RSS: Using RSS feed");

            try
            {
                var response = await httpClient.GetAsync(rssUrl);
                crawledInfo.HttpStatus = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    crawledInfo.Success = false;
                    crawledInfo.ErrorMessage = $"HTTP {(int)response.StatusCode}";
                    summary.DebugInfo.Add($"[{site.Name}] RSS: HTTP {(int)response.StatusCode}");
                    return results;
                }

                var xml = await response.Content.ReadAsStringAsync();
                crawledInfo.Success = true;

                // Parse RSS/Newznab XML response - reuse the existing parser
                results = ParseRssFeedResponse(xml, site.Name ?? "Unknown", rssUrl, unwatchedEpisodes, summary);
                summary.DebugInfo.Add($"[{site.Name}] RSS: Found {results.Count} results");
            }
            catch (Exception ex)
            {
                crawledInfo.Success = false;
                crawledInfo.ErrorMessage = ex.Message;
                summary.DebugInfo.Add($"[{site.Name}] RSS Error: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Parse RSS feed XML response. Similar to ParseNewznabResponse but handles RSS-specific format.
        /// </summary>
        internal List<NzbSiteCrawlResult> ParseRssFeedResponse(
            string xml,
            string siteName,
            string feedUrl,
            List<Episode> unwatchedEpisodes,
            NzbSiteCrawlSummary summary)
        {
            var results = new List<NzbSiteCrawlResult>();

            // Build unwatched episode codes
            var unwatchedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ep in unwatchedEpisodes)
            {
                unwatchedCodes.Add(ep.EpNumberFormatted);
                unwatchedCodes.Add($"S{ep.season}E{ep.number}");
                unwatchedCodes.Add($"{ep.season}x{ep.number:D2}");
            }

            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xml);

                // Check for error response
                var error = doc.Descendants("error").FirstOrDefault();
                if (error != null)
                {
                    var errorCode = error.Attribute("code")?.Value;
                    var errorDesc = error.Attribute("description")?.Value;
                    summary.DebugInfo.Add($"[{siteName}] RSS Error: {errorCode} - {errorDesc}");
                    return results;
                }

                // Parse items (RSS feed items are typically under <channel><item>)
                var items = doc.Descendants("item");
                summary.DebugInfo.Add($"[{siteName}] RSS: Parsing {items.Count()} items from feed");

                foreach (var item in items)
                {
                    var title = item.Element("title")?.Value ?? "";

                    // RSS feeds may have link or enclosure for download URL
                    var link = item.Element("link")?.Value ?? "";
                    var enclosure = item.Element("enclosure");
                    if (enclosure != null)
                    {
                        var enclosureUrl = enclosure.Attribute("url")?.Value;
                        if (!string.IsNullOrEmpty(enclosureUrl))
                            link = enclosureUrl;
                    }

                    var pubDate = item.Element("pubDate")?.Value;
                    var size = "";

                    // Get size from enclosure or newznab attributes
                    if (enclosure != null)
                    {
                        var lengthAttr = enclosure.Attribute("length")?.Value;
                        if (!string.IsNullOrEmpty(lengthAttr) && long.TryParse(lengthAttr, out var bytes))
                        {
                            size = bytes > 1_000_000_000 ? $"{bytes / 1_000_000_000.0:F2} GB" : $"{bytes / 1_000_000.0:F1} MB";
                        }
                    }

                    // Also check newznab:attr elements (some feeds include these)
                    var attrs = item.Elements().Where(e => e.Name.LocalName == "attr");
                    foreach (var attr in attrs)
                    {
                        var name = attr.Attribute("name")?.Value;
                        var value = attr.Attribute("value")?.Value;
                        if (name == "size" && string.IsNullOrEmpty(size) && !string.IsNullOrEmpty(value) && long.TryParse(value, out var bytes))
                        {
                            size = bytes > 1_000_000_000 ? $"{bytes / 1_000_000_000.0:F2} GB" : $"{bytes / 1_000_000.0:F1} MB";
                        }
                    }

                    // Extract episode code from title
                    string? episodeCode = null;
                    var epMatch = Regex.Match(title, @"S(\d{1,2})E(\d{1,2})", RegexOptions.IgnoreCase);
                    if (epMatch.Success)
                    {
                        var season = int.Parse(epMatch.Groups[1].Value);
                        var episode = int.Parse(epMatch.Groups[2].Value);
                        episodeCode = $"S{season:D2}E{episode:D2}";
                    }
                    else
                    {
                        var altMatch = Regex.Match(title, @"(\d{1,2})x(\d{2})", RegexOptions.IgnoreCase);
                        if (altMatch.Success)
                        {
                            var season = int.Parse(altMatch.Groups[1].Value);
                            var episode = int.Parse(altMatch.Groups[2].Value);
                            episodeCode = $"S{season:D2}E{episode:D2}";
                        }
                    }

                    if (string.IsNullOrEmpty(episodeCode)) continue;
                    if (!unwatchedCodes.Contains(episodeCode)) continue;

                    results.Add(new NzbSiteCrawlResult
                    {
                        SiteName = siteName,
                        Title = title.Length > 150 ? title.Substring(0, 150) + "..." : title,
                        EpisodeCode = episodeCode,
                        DownloadUrl = link,
                        Size = size,
                        PostDate = DateTime.TryParse(pubDate, out var dt) ? dt : null,
                        SearchUrl = RedactUrlParam(feedUrl, @"r=[^&]+", "r=[HIDDEN]")
                    });
                }
            }
            catch (Exception ex)
            {
                summary.DebugInfo.Add($"[{siteName}] RSS XML Parse Error: {ex.Message}");
            }

            return results;
        }

        // ── Existing folder detection for undecided shows ──

        public List<ExistingFolderMatch> FindExistingFolders(Show show, List<ShowFolderAlias> aliases)
        {
            using var _db = _dbFactory.CreateDbContext();
            var root = _options.TvNameListPath;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return new();

            // Build set of names to match (case-insensitive)
            var namesToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(show.name))
                namesToMatch.Add(show.name);

            // Add all folder name variants and year permutations
            var yearRegexBare = new System.Text.RegularExpressions.Regex(@"^(.+)\s+(\d{4})$");
            var yearRegexParen = new System.Text.RegularExpressions.Regex(@"^(.+)\s+\((\d{4})\)$");
            var folderCandidates = new[] { show.DefaultFolderName, show.SuggestedFolderName, show.FolderName };
            foreach (var folder in folderCandidates)
            {
                if (string.IsNullOrEmpty(folder)) continue;
                namesToMatch.Add(folder);

                // Extract base name and year from "Name 2025" or "Name (2025)"
                string baseName = null;
                string year = null;
                var m = yearRegexParen.Match(folder);
                if (m.Success)
                {
                    baseName = m.Groups[1].Value.Trim();
                    year = m.Groups[2].Value;
                }
                else
                {
                    m = yearRegexBare.Match(folder);
                    if (m.Success)
                    {
                        baseName = m.Groups[1].Value.Trim();
                        year = m.Groups[2].Value;
                    }
                }

                if (!string.IsNullOrEmpty(baseName) && !string.IsNullOrEmpty(year))
                {
                    namesToMatch.Add(baseName);                  // "Show Name"
                    namesToMatch.Add($"{baseName} {year}");      // "Show Name 2025"
                    namesToMatch.Add($"{baseName} ({year})");    // "Show Name (2025)"
                }
            }

            if (aliases != null)
            {
                foreach (var a in aliases)
                    if (!string.IsNullOrEmpty(a.AliasName))
                        namesToMatch.Add(a.AliasName);
            }

            // Collect all root paths to scan
            var rootPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(root))
                rootPaths.Add(root);

            // Also scan TV directories that have DaysToScan > 0
            foreach (var tvDir in _db.TVDirectories.Where(d => d.DaysToScan > 0))
            {
                if (!string.IsNullOrEmpty(tvDir.Name) && Directory.Exists(tvDir.Name))
                    rootPaths.Add(tvDir.Name);
            }

            var results = new List<ExistingFolderMatch>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var scanRoot in rootPaths)
            {
                foreach (var dir in Directory.GetDirectories(scanRoot))
                {
                    var folderName = Path.GetFileName(dir);
                    if (folderName == null || !namesToMatch.Contains(folderName))
                        continue;

                    // Avoid duplicates if the same folder is reachable from multiple roots
                    if (!seen.Add(dir))
                        continue;

                    var match = new ExistingFolderMatch
                    {
                        FolderName = folderName,
                        FullPath = dir,
                        FolderDate = Directory.GetLastWriteTime(dir)
                    };

                    // Scan files for earliest/latest episode
                    try
                    {
                        var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                        long? minSeason = null, minEp = null, maxSeason = null, maxEp = null;
                        foreach (var file in files)
                        {
                            var parsedList = EpisodeNameParser.Parse(Path.GetFileName(file));
                            if (parsedList == null) continue;
                            foreach (var (s, e) in parsedList)
                            {
                                if (minSeason == null || s < minSeason || (s == minSeason && e < minEp))
                                {
                                    minSeason = s;
                                    minEp = e;
                                }
                                if (maxSeason == null || s > maxSeason || (s == maxSeason && e > maxEp))
                                {
                                    maxSeason = s;
                                    maxEp = e;
                                }
                            }
                        }
                        if (minSeason != null)
                            match.EarliestEpisode = $"S{minSeason:D2}E{minEp:D2}";
                        if (maxSeason != null)
                            match.LatestEpisode = $"S{maxSeason:D2}E{maxEp:D2}";
                    }
                    catch { }

                    results.Add(match);
                }
            }

            return results.OrderBy(r => r.FolderName).ToList();
        }

        // ── Show folder aliases ──

        public List<ShowFolderAlias> GetFolderAliases(long showId)
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.ShowFolderAliases
                .Where(a => a.ShowId == (int)showId)
                .OrderBy(a => a.AliasName)
                .ToList();
        }

        public async Task AddFolderAlias(long showId, string aliasName, int seasonOffset = 0)
        {
            using var _db = _dbFactory.CreateDbContext();
            if (string.IsNullOrWhiteSpace(aliasName)) return;

            var existing = _db.ShowFolderAliases
                .FirstOrDefault(a => a.ShowId == (int)showId && a.AliasName == aliasName.Trim());
            if (existing != null)
            {
                // Allow updating the offset on an existing alias.
                if (existing.SeasonOffset != seasonOffset)
                {
                    existing.SeasonOffset = seasonOffset;
                    await _db.SaveChangesAsync();
                }
                return;
            }

            _db.ShowFolderAliases.Add(new ShowFolderAlias
            {
                ShowId = (int)showId,
                AliasName = aliasName.Trim(),
                SeasonOffset = seasonOffset
            });
            await _db.SaveChangesAsync();
        }

        public async Task RemoveFolderAlias(int aliasId)
        {
            using var _db = _dbFactory.CreateDbContext();
            var alias = _db.ShowFolderAliases.Find(aliasId);
            if (alias != null)
            {
                _db.ShowFolderAliases.Remove(alias);
                await _db.SaveChangesAsync();
            }
        }

        // Friends management

        public List<Friend> GetFriends()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.Friends
                .Include(f => f.InterestedShows)
                    .ThenInclude(fs => fs.Show)
                .OrderBy(f => f.Name)
                .ToList();
        }

        public async Task<Friend> AddFriend(string name, string email, string folderPath)
        {
            using var _db = _dbFactory.CreateDbContext();
            var friend = new Friend
            {
                Name = (name ?? "").Trim(),
                Email = (email ?? "").Trim(),
                FolderPath = (folderPath ?? "").Trim()
            };
            _db.Friends.Add(friend);
            await _db.SaveChangesAsync();
            return friend;
        }

        public async Task UpdateFriend(int id, string name, string email, string folderPath)
        {
            using var _db = _dbFactory.CreateDbContext();
            var friend = _db.Friends.Find(id);
            if (friend != null)
            {
                friend.Name = (name ?? "").Trim();
                friend.Email = (email ?? "").Trim();
                friend.FolderPath = (folderPath ?? "").Trim();
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteFriend(int id)
        {
            using var _db = _dbFactory.CreateDbContext();
            var friend = _db.Friends
                .Include(f => f.InterestedShows)
                .FirstOrDefault(f => f.Id == id);
            if (friend != null)
            {
                _db.FriendShows.RemoveRange(friend.InterestedShows);
                var copies = _db.FriendCopies.Where(c => c.FriendId == id);
                _db.FriendCopies.RemoveRange(copies);
                _db.Friends.Remove(friend);
                await _db.SaveChangesAsync();
            }
        }

        public async Task AddFriendShow(int friendId, int showId)
        {
            using var _db = _dbFactory.CreateDbContext();
            bool exists = _db.FriendShows.Any(fs => fs.FriendId == friendId && fs.ShowId == showId);
            if (!exists)
            {
                _db.FriendShows.Add(new FriendShow { FriendId = friendId, ShowId = showId });
                await _db.SaveChangesAsync();
            }
        }

        public async Task RemoveFriendShow(int friendShowId)
        {
            using var _db = _dbFactory.CreateDbContext();
            var fs = _db.FriendShows.Find(friendShowId);
            if (fs != null)
            {
                _db.FriendShows.Remove(fs);
                await _db.SaveChangesAsync();
            }
        }

        public List<Show> GetWatchedShows()
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.Shows
                .Where(s => s.Episodes.Any(e => e.Watched))
                .OrderBy(s => s.name)
                .AsNoTracking()
                .ToList();
        }

        public List<FriendCopy> GetRecentCopiesForFriend(int friendId, int count = 10)
        {
            using var _db = _dbFactory.CreateDbContext();
            return _db.FriendCopies
                .Where(c => c.FriendId == friendId)
                .OrderByDescending(c => c.CopiedAt)
                .Take(count)
                .ToList();
        }

        // Show predecessor/successor links

        public List<ShowLink> GetShowLinks(long showId)
        {
            using var _db = _dbFactory.CreateDbContext();
            int id = (int)showId;
            return _db.ShowLinks
                .Include(sl => sl.PredecessorShow)
                .Include(sl => sl.SuccessorShow)
                .Where(sl => sl.PredecessorShowId == id || sl.SuccessorShowId == id)
                .ToList();
        }

        public async Task AddShowLink(long predecessorShowId, long successorShowId)
        {
            using var _db = _dbFactory.CreateDbContext();
            int predId = (int)predecessorShowId;
            int succId = (int)successorShowId;

            bool exists = _db.ShowLinks.Any(sl =>
                (sl.PredecessorShowId == predId && sl.SuccessorShowId == succId) ||
                (sl.PredecessorShowId == succId && sl.SuccessorShowId == predId));

            if (!exists)
            {
                _db.ShowLinks.Add(new ShowLink
                {
                    PredecessorShowId = predId,
                    SuccessorShowId = succId
                });
                await _db.SaveChangesAsync();
            }
        }

        public async Task RemoveShowLink(int showLinkId)
        {
            using var _db = _dbFactory.CreateDbContext();
            var link = _db.ShowLinks.Find(showLinkId);
            if (link != null)
            {
                _db.ShowLinks.Remove(link);
                await _db.SaveChangesAsync();
            }
        }
    }
}

