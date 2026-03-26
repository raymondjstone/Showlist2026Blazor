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
        private readonly ShowlistDbContext _db;
        private readonly ILogger<ShowListAppService> _logger;
        private readonly ShowlistOptions _options;
        private readonly INotificationService _notifications;


        public ShowListAppService(ShowlistDbContext db, ILogger<ShowListAppService> logger,
            IOptions<ShowlistOptions> options, INotificationService notifications)
        {
            _db = db;
            _logger = logger;
            _options = options.Value;
            _notifications = notifications;
        }

        public List<Show> showlist(string srch)
        {
            return _db.Shows.Where(s => s.name.Contains(srch)).OrderBy(a => a.name).ToList();
        }

        public List<TVSite> TvSites()
        {
            return _db.TVSites.OrderBy(a => a.Order).ToList();
        }

        public async Task TVSiteUpdate(int id, bool active, int order, string name, string urltemplate)
        {
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
                    Active = active
                };
                _db.Add(newOne);
            }
            else
            {
                current.Order = order;
                current.Name = name;
                current.URLTemplate = @urltemplate;
                current.Active = active;
                _db.Update(current);
            }

            await _db.SaveChangesAsync();


        }

        public async Task TVSiteDelete(int id)
        {
            var site = _db.TVSites.Find(id);
            if (site != null)
            {
                _db.TVSites.Remove(site);
                await _db.SaveChangesAsync();
            }
        }

        public List<TVDirectories> TvDirectories()
        {
            return _db.TVDirectories.OrderBy(a => a.Name).ToList();
        }

        public async Task TVDirectoryUpdate(int id, string name, int daysToScan, string filter, int minFileSize)
        {
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
                    MinFileSize = minFileSize
                };
                _db.Add(newOne);
            }
            else
            {
                current.Name = name;
                current.DaysToScan = daysToScan;
                current.Filter = filter;
                current.MinFileSize = minFileSize;
                _db.Update(current);
            }

            await _db.SaveChangesAsync();
        }

        public async Task TVDirectoryDelete(int id)
        {
            var dir = _db.TVDirectories.Find(id);
            if (dir != null)
            {
                _db.TVDirectories.Remove(dir);
                await _db.SaveChangesAsync();
            }
        }

        public List<Country> CountryData()
        {
            return _db.Countrys.ToList() ?? new List<Country>();
        }

        public List<Language> LanguageData()
        {
            return _db.Languages.ToList() ?? new List<Language>();
        }

        public List<Type> TypeData()
        {
            return _db.Types.ToList() ?? new List<Type>();
        }

        public List<GenreText> GenreData()
        {
            return _db.GenreTexts.ToList() ?? new List<GenreText>();
        }

        public List<Network> NetworkData()
        {
            return _db.Networks.Include(n => n.country).ToList() ?? new List<Network>();
        }

        public List<WebNetwork> WebNetworkData()
        {
            return _db.WebNetworks.Include(n => n.country).ToList() ?? new List<WebNetwork>();
        }

        public List<Show> ShowData()
        {
            return _db.Shows.Where(s => s.Wanted != null).OrderBy(s => s.name).ToList();
        }



        public Show ShowPageData(long id)
        {

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

                PopulateSuggestedFolderNames(new List<Show> { show });
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
            var sw = System.Diagnostics.Stopwatch.StartNew();


            //Used to use a view but that ended up being too restrictive
            DateTimeOffset min = DateTimeOffset.UtcNow.AddDays(daysminus);
            DateTimeOffset max = DateTimeOffset.UtcNow.AddDays(daysplus);

            _db.Database.SetCommandTimeout(120);

            // Load all user filter selections first (small datasets, fast queries)
            var showFilters = _db.Shows.Where(s => s.Wanted != null)
                .Select(s => new { s.Id, Include = s.Wanted.Value })
                .ToDictionary(s => s.Id, s => s.Include);
            var networkFilterMap = _db.Networks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var webnetworkFilterMap = _db.WebNetworks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var genreFilterMap = _db.GenreTexts.Where(g => g.Wanted != null).ToDictionary(g => g.Id, g => g.Wanted.Value);
            var languageFilterMap = _db.Languages.Where(l => l.Wanted != null).ToDictionary(l => l.Id, l => l.Wanted.Value);
            var typeFilterMap = _db.Types.Where(t => t.Wanted != null).ToDictionary(t => t.Id, t => t.Wanted.Value);
            var countryFilterMap = _db.Countrys.Where(c => c.Wanted != null).ToDictionary(c => c.Id, c => c.Wanted.Value);
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
            var selectedNetworkIds = networkFilterMap.Where(n => n.Value).Select(n => n.Key).ToHashSet();
            var selectedWebNetworkIds = webnetworkFilterMap.Where(n => n.Value).Select(n => n.Key).ToHashSet();
            var selectedTypeIds = typeFilterMap.Where(t => t.Value).Select(t => t.Key).ToHashSet();
            var selectedLangIds = languageFilterMap.Where(l => l.Value).Select(l => l.Key).ToHashSet();
            var selectedCountryIds = countryFilterMap.Where(c => c.Value).Select(c => c.Key).ToHashSet();
            var selectedGenreTextIds = genreFilterMap.Where(g => g.Value).Select(g => g.Key).ToHashSet();

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

            // Query 2: S01E01 "new show" discovery (limited to last 90 days for performance)
            var recentMin = DateTimeOffset.UtcNow.AddDays(-90);
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
                var ef = CreateEpFilter(e, showFilterMap, networkFilterMap, webnetworkFilterMap,
                        genreFilterMap, languageFilterMap, typeFilterMap, countryFilterMap, tvsites);
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



        private void PopulateSuggestedFolderNames(List<Show> shows)
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

        private EpFilter CreateEpFilter(
                Episode e,
                Dictionary<int, bool> showFilterMap,
                Dictionary<int, bool> networkFilterMap,
                Dictionary<int, bool> webnetworkFilterMap,
                Dictionary<int, bool> genreFilterMap,
                Dictionary<int, bool> languageFilterMap,
                Dictionary<int, bool> typeFilterMap,
                Dictionary<int, bool> countryFilterMap,
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

                if (ef.ep.show.Types != null && typeFilterMap.TryGetValue(ef.ep.show.Types.Id, out var typeInc))
                {
                    ef.typeinclude = typeInc;
                    if (typeInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!typeInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.Networks != null && networkFilterMap.TryGetValue(ef.ep.show.Networks.Id, out var netInc))
                {
                    ef.networkinclude = netInc;
                    if (netInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!netInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.WebNetworks != null && webnetworkFilterMap.TryGetValue(ef.ep.show.WebNetworks.Id, out var webInc))
                {
                    ef.webnetworkinclude = webInc;
                    if (webInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!webInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.Languages != null && languageFilterMap.TryGetValue(ef.ep.show.Languages.Id, out var langInc))
                {
                    ef.languageinclude = langInc;
                    if (langInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!langInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                //Country filters checked against both main Network and WebNetwork in turn
                if (ef.ep.show.Networks?.country != null && countryFilterMap.TryGetValue(ef.ep.show.Networks.country.Id, out var cntInc))
                {
                    ef.countryinclude = cntInc;
                    if (cntInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!cntInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }
                if (ef.ep.show.WebNetworks?.country != null && countryFilterMap.TryGetValue(ef.ep.show.WebNetworks.country.Id, out var wcntInc))
                {
                    if (ef.countryinclude == null) ef.countryinclude = wcntInc;
                    if (wcntInc && !ef.AlreadyDecidedUpon) ef.Activelyselected = true;
                    if (!wcntInc && !ef.AlreadyDecidedUpon) ef.Activelyignored = true;
                }

                if (ef.ep.show.Genres != null)
                {
                    foreach (var g in ef.ep.show.Genres)
                    {
                        if (g.genretext != null && genreFilterMap.TryGetValue(g.genretext.Id, out var gInc))
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
            var sw = System.Diagnostics.Stopwatch.StartNew();


            // Shows the user has already made a decision on (wanted or unwanted)
            var decidedShowIds = _db.Shows
                .Where(s => s.Wanted != null)
                .Select(s => s.Id)
                .ToHashSet();
            _logger.LogDebug($"PERF[UndecidedShows] Decided shows loaded ({decidedShowIds.Count}): {sw.ElapsedMilliseconds}ms");

            // Load user filter exclusions directly from main entity tables
            var networkFilterMap = _db.Networks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var webnetworkFilterMap = _db.WebNetworks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var genreFilterMap = _db.GenreTexts.Where(g => g.Wanted != null).ToDictionary(g => g.Id, g => g.Wanted.Value);
            var languageFilterMap = _db.Languages.Where(l => l.Wanted != null).ToDictionary(l => l.Id, l => l.Wanted.Value);
            var typeFilterMap = _db.Types.Where(t => t.Wanted != null).ToDictionary(t => t.Id, t => t.Wanted.Value);
            var countryFilterMap = _db.Countrys.Where(c => c.Wanted != null).ToDictionary(c => c.Id, c => c.Wanted.Value);

            // Build sets of excluded IDs from filters
            var excludedNetworkIds = networkFilterMap.Where(n => !n.Value).Select(n => n.Key).ToHashSet();
            var excludedWebNetworkIds = webnetworkFilterMap.Where(n => !n.Value).Select(n => n.Key).ToHashSet();
            var excludedTypeIds = typeFilterMap.Where(t => !t.Value).Select(t => t.Key).ToHashSet();
            var excludedLangIds = languageFilterMap.Where(l => !l.Value).Select(l => l.Key).ToHashSet();
            var excludedCountryIds = countryFilterMap.Where(c => !c.Value).Select(c => c.Key).ToHashSet();
            var excludedGenreTextIds = genreFilterMap.Where(g => !g.Value).Select(g => g.Key).ToHashSet();
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
                    CreateEpFilter(e, showFilterMap, networkFilterMap, webnetworkFilterMap,
                        genreFilterMap, languageFilterMap, typeFilterMap, countryFilterMap, tvsites)
                );
            }

            var result = EpFilters.ToList();
            _logger.LogDebug($"PERF[UndecidedShows] TOTAL: {sw.ElapsedMilliseconds}ms | {result.Count} results");
            return result;
        }

        public List<EpFilter> NextUnwatchedPerShow()
        {
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
            var x = _db.Shows
                    .Where(s => s.Wanted == true)
                   .OrderBy(s => s.name);

            var results = x.Where(s => string.IsNullOrEmpty(s.FolderName)).ToList();
            PopulateSuggestedFolderNames(results);
            return results;
        }


        public List<ShowFilter> ComingSoonForUser(int daysminus = 1, int daysplus = 366)
        {


            //Used to use a view but that ended up being too restrictive
            DateTimeOffset min = DateTimeOffset.UtcNow.AddDays(daysminus);
            DateTimeOffset max = DateTimeOffset.UtcNow.AddDays(daysplus);

            string yeara = "/" + min.Year.ToString();
            string yearb = "/" + max.Year.ToString();

            var eps1 = _db.Shows.Where(a => a.premiered.Contains(yeara) || a.premiered.Contains(yearb)).ToList();

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
            var networkFilterMap = _db.Networks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var webnetworkFilterMap = _db.WebNetworks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var genreFilterMap = _db.GenreTexts.Where(g => g.Wanted != null).ToDictionary(g => g.Id, g => g.Wanted.Value);
            var languageFilterMap = _db.Languages.Where(l => l.Wanted != null).ToDictionary(l => l.Id, l => l.Wanted.Value);
            var typeFilterMap = _db.Types.Where(t => t.Wanted != null).ToDictionary(t => t.Id, t => t.Wanted.Value);
            var countryFilterMap = _db.Countrys.Where(c => c.Wanted != null).ToDictionary(c => c.Id, c => c.Wanted.Value);

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

                if (!decided && ef.ep.Types != null && typeFilterMap.TryGetValue(ef.ep.Types.Id, out var typeInc))
                {
                    ef.typeinclude = typeInc;
                    if (typeInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.Networks != null && networkFilterMap.TryGetValue(ef.ep.Networks.Id, out var netInc))
                {
                    ef.networkinclude = netInc;
                    if (netInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.WebNetworks != null && webnetworkFilterMap.TryGetValue(ef.ep.WebNetworks.Id, out var webInc))
                {
                    ef.webnetworkinclude = webInc;
                    if (webInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.Languages != null && languageFilterMap.TryGetValue(ef.ep.Languages.Id, out var langInc))
                {
                    ef.languageinclude = langInc;
                    if (langInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }

                if (!decided && ef.ep.Networks?.country != null && countryFilterMap.TryGetValue(ef.ep.Networks.country.Id, out var cntInc))
                {
                    ef.countryinclude = cntInc;
                    if (cntInc) ef.activelyselected = true;
                    else ef.activelyignored = true;
                    decided = true;
                }
                if (!decided && ef.ep.WebNetworks?.country != null && countryFilterMap.TryGetValue(ef.ep.WebNetworks.country.Id, out var wcntInc))
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
                        if (g.genretext != null && genreFilterMap.TryGetValue(g.genretext.Id, out var gInc))
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
            try
            {
                var show = _db.Shows.FirstOrDefault(a => a.Id == id);
                if (show == null) return false;

                var wasUndecided = show.Wanted == null;
                show.Wanted = statewanted;
                await _db.SaveChangesAsync();

                // Create folder when newly marking as wanted
                if (statewanted == true && wasUndecided)
                {
                    try
                    {
                        string f = show.FolderName;
                        if (string.IsNullOrEmpty(f))
                        {
                            f = show.DefaultFolderName;
                        }
                        //   F:\tv_name_list
                        string path = Path.Combine(_options.ShowFolderBasePath, show.DefaultFolderName);
                        if (!(Directory.Exists(path)))
                        {
                            Directory.CreateDirectory(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create folder for show {ShowId}", id);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }


        public async Task<bool> LanguageFilter(long id, bool? statewanted)
        {
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
                Console.WriteLine(e);
                return false;
            }
        }


        public async Task<bool> TypeFilter(long id, bool? statewanted)
        {
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
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> NetworkFilter(long id, bool? statewanted)
        {
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
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> WebNetworkFilter(long id, bool? statewanted)
        {
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
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> GenreFilter(long id, bool? statewanted)
        {
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
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> CountryFilter(long id, bool? statewanted)
        {
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
                Console.WriteLine(e);
                return false;
            }
        }

        public async Task<bool> SeasonWatchedFilter(long id, long season, bool statewanted)
        {


            var s = _db.Shows.Find((int)id);
            var ee = _db.Episodes.Where(x => x.show.Id == s.Id).ToList();



            if (s != null)
            {
                var sfirst = s.Episodes.Where(e => e.season == season).ToList();
                foreach (var V in sfirst)
                {
                    await WatchedFilter(V.Id, statewanted);
                }
            }

            return true;

        }






        public async Task<bool> WatchedFilter(long id, bool statewanted)
        {
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
                Console.WriteLine(e);
                return false;
            }
        }

        public List<EpFilter> MissedEpisodes()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
                .Include(e => e.show.WebNetworks)
                .Include(e => e.show.Networks)
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
            var networkFilterMap = _db.Networks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var webnetworkFilterMap = _db.WebNetworks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var genreFilterMap = _db.GenreTexts.Where(g => g.Wanted != null).ToDictionary(g => g.Id, g => g.Wanted.Value);
            var languageFilterMap = _db.Languages.Where(l => l.Wanted != null).ToDictionary(l => l.Id, l => l.Wanted.Value);
            var typeFilterMap = _db.Types.Where(t => t.Wanted != null).ToDictionary(t => t.Id, t => t.Wanted.Value);
            var countryFilterMap = _db.Countrys.Where(c => c.Wanted != null).ToDictionary(c => c.Id, c => c.Wanted.Value);

            var tvsites = TvSites();

            var result = new List<EpFilter>();
            foreach (var e in eps.Where(a => a.show != null))
            {
                var ef = CreateEpFilter(e, showFilterMap, networkFilterMap, webnetworkFilterMap,
                    genreFilterMap, languageFilterMap, typeFilterMap, countryFilterMap, tvsites);
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
                Console.WriteLine(e);
                return false;
            }
        }

        public List<EpFilter> GivenUpEpisodes()
        {
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
            var networkFilterMap = _db.Networks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var webnetworkFilterMap = _db.WebNetworks.Where(n => n.Wanted != null).ToDictionary(n => n.Id, n => n.Wanted.Value);
            var genreFilterMap = _db.GenreTexts.Where(g => g.Wanted != null).ToDictionary(g => g.Id, g => g.Wanted.Value);
            var languageFilterMap = _db.Languages.Where(l => l.Wanted != null).ToDictionary(l => l.Id, l => l.Wanted.Value);
            var typeFilterMap = _db.Types.Where(t => t.Wanted != null).ToDictionary(t => t.Id, t => t.Wanted.Value);
            var countryFilterMap = _db.Countrys.Where(c => c.Wanted != null).ToDictionary(c => c.Id, c => c.Wanted.Value);

            var tvsites = TvSites();

            var result = new List<EpFilter>();
            foreach (var e in eps.Where(a => a.show != null))
            {
                var ef = CreateEpFilter(e, showFilterMap, networkFilterMap, webnetworkFilterMap,
                    genreFilterMap, languageFilterMap, typeFilterMap, countryFilterMap, tvsites);
                result.Add(ef);
            }

            return result.OrderByDescending(a => a.ep.AiringTime).ThenBy(a => a.ep.show.name).ToList();
        }

        public async Task<bool> SetFolderName(long id, string foldername)
        {

            var show = _db.Shows.Find((int)id);
            if (show != null)
            {
                show.FolderName = foldername;
                _db.Update(show);
                await _db.SaveChangesAsync();
            }
            return true;
        }


        public async Task<List<FileInfo>> Dirlist(string dirName, int daysOldToAllow, string filter = "*.*", int minSizeAllowed = 50000)
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
            return filesList.OrderByDescending(f => f.LastWriteTime).ToList();

        }


        public async Task<List<TouchFile>> ShowDownloaded(int years = 0)
        {
            var downloads = _db.TouchFiles
                .OrderByDescending(r => r.FileDate)
                .Include(a => a.Episode)
                .Include(a => a.Episode.show);

            return downloads.Where(f => f.FileDate.Year == years || years == 0).ToList();
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
            if (!string.IsNullOrEmpty(show.showid.ToString()))
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
            var stats = new StatisticsModel();


            var wantedShows = _db.Shows
                .Where(s => s.Wanted == true)
                .ToList();

            var wantedShowIds = wantedShows.Select(s => s.Id).ToHashSet();
            stats.TotalShowsTracked = wantedShowIds.Count;

            stats.ActiveShows = wantedShows.Count(s => s.status != null && s.status != "Ended");
            stats.CompletedShows = wantedShows.Count(s => s.status == "Ended");

            var watchedEps = _db.Episodes
                .Where(e => e.Watched)
                .Include(e => e.show)
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
                .Where(g => g.show != null && watchedShowIds.Contains(g.show.Id) && g.genretext != null)
                .ToList()
                .GroupBy(g => g.genretext.genre)
                .ToDictionary(g => g.Key, g => g.Select(x => x.show.Id).Distinct().Count());
            stats.GenreBreakdown = genres;

            // Most watched shows
            stats.MostWatchedShows = watchedEps
                .Where(e => e.show != null)
                .GroupBy(e => e.show.Id)
                .Select(g => new ShowWatchStat
                {
                    ShowId = g.Key,
                    ShowName = g.First().show.name ?? "",
                    EpisodesWatched = g.Count(),
                    TotalEpisodes = _db.Episodes.Count(e => e.show.Id == g.Key)
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
                .ToList();

            return (results, totalCount);
        }

        // ===== Feature 4: Bulk actions =====
        public async Task BulkSetShowFilter(List<long> showIds, bool? state)
        {


            var shows = _db.Shows.Where(s => showIds.Contains(s.Id)).ToList();
            foreach (var show in shows)
            {
                show.Wanted = state;
            }

            await _db.SaveChangesAsync();
        }

        public async Task CatchUpShow(long showId)
        {
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
            var show = _db.Shows.Find((int)showId);
            if (show != null)
            {
                show.Priority = priority;
                await _db.SaveChangesAsync();
            }
        }

        public Dictionary<int, (int watched, int total)> GetEpisodeCountsForShows(List<int> showIds)
        {
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
            var dupeShowIds = _db.Shows
                .GroupBy(s => s.showid)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Select(s => s.Id))
                .ToList();

            return _db.Shows.Where(s => dupeShowIds.Contains(s.Id))
                .OrderBy(s => s.showid).ThenBy(s => s.Id)
                .ToList();
        }

        public async Task<List<TrendingShowModel>> GetTrendingShows()
        {
            try
            {
                var schedule = await ($"{_options.TvMazeBaseUrl}/schedule")
                    .GetJsonAsync<List<System.Text.Json.JsonElement>>();

                var localShowIds = _db.Shows.Select(s => s.showid).ToHashSet();
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
                        LocalShowId = localShowIds.Contains(id) ? (int?)_db.Shows.FirstOrDefault(s => s.showid == id)?.Id : null
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
            var model = new ShowComparisonModel();
            model.Show1 = BuildComparisonSide(showId1);
            model.Show2 = BuildComparisonSide(showId2);
            return model;
        }

        private ShowComparisonSide BuildComparisonSide(long showId)
        {
            var show = _db.Shows
                .Include(s => s.Episodes)
                .Include(s => s.Networks)
                .Include(s => s.WebNetworks)
                .Include(s => s.Genres).ThenInclude(g => g.genretext)
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
            var model = new StorageDashboardModel();

            var tvDirs = _db.TVDirectories
                .Where(d => d.DaysToScan != 0)
                .ToList();

            if (!tvDirs.Any())
                return model;

            // Build folder -> show lookup
            var allShows = _db.Shows.ToList();
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

        private (Dictionary<string, Show> folderLookup, List<ParsedPathLine> parsed, int linesSkipped, HashSet<string> unmatchedFolders) ParseImportPaths(string fileContent)
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

                var epParsed = EpisodeNameParser.Parse(fileName);
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
            var (folderLookup, parsed, linesSkipped, unmatchedFolders) = ParseImportPaths(fileContent);

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
            var (folderLookup, parsed, _, _) = ParseImportPaths(fileContent);

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
            var dirs = _db.TVDirectories
                .Where(d => d.DaysToScan != 0)
                .ToList();

            var allShows = _db.Shows.Where(s => s.Wanted == true).ToList();

            var allFiles = new List<DuplicateFileEntry>();

            foreach (var tvdir in dirs)
            {
                if (string.IsNullOrWhiteSpace(tvdir.Name) || !Directory.Exists(tvdir.Name.Trim()))
                    continue;

                List<FileInfo> files;
                try
                {
                    files = Directory.GetFiles(tvdir.Name.Trim(), tvdir.Filter ?? "*.*", SearchOption.AllDirectories)
                        .Select(f => new FileInfo(f))
                        .Where(fi => fi.Length > 0)
                        .ToList();
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var fi in files)
                {
                    if (fi.DirectoryName == null || fi.DirectoryName.Length <= 5) continue;

                    var parsed = EpisodeNameParser.Parse(fi.Name);
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
                        FileSize = fi.Length
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
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return false;

            File.Delete(filePath);
            return true;
        }
    }
}

