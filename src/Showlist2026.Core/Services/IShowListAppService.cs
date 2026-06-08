using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Showlist2026.Entities;
using Showlist2026.Models;
using Showlist2026.NZBPlanetApiJSON;
using Showlist2026.TVMaze;

namespace Showlist2026.Services
{
    public interface IShowListAppService
    {
        HomePageStats HomePageStats();
        List<EpFilter> AiringAroundNowForUser(int daysminus = -15, int daysplus = 15, bool firstshowOnly = false, bool includeIgnored = false, bool includeWatched = false);
        List<EpFilter> UndecidedShows();
        List<EpFilter> NextUnwatchedPerShow();
        List<ShowFilter> ComingSoonForUser(int daysminus = 1, int daysplus = 366);
        List<Show> NoFolderList();
        Show ShowPageData(long id);
        List<TVSite> TvSites();
        List<Show> ShowData();
        List<Country> CountryData();
        List<Language> LanguageData();
        List<Showlist2026.Entities.Type> TypeData();
        List<Network> NetworkData();
        List<WebNetwork> WebNetworkData();
        List<GenreText> GenreData();
        List<Show> showlist(string s);
        Task<bool> ShowFilter(long id, bool? statewanted);
        Task<bool> LanguageFilter(long id, bool? statewanted);
        Task<bool> CountryFilter(long id, bool? statewanted);
        Task<bool> NetworkFilter(long id, bool? statewanted);
        Task<bool> WebNetworkFilter(long id, bool? statewanted);
        Task<bool> TypeFilter(long id, bool? statewanted);
        Task<bool> GenreFilter(long id, bool? statewanted);
        Task<bool> WatchedFilter(long id, bool statewanted);
        Task<bool> SeasonWatchedFilter(long id, long season, bool statewanted);
        Task<bool> GivenUpFilter(long id, bool statewanted);
        List<EpFilter> MissedEpisodes();
        List<EpFilter> GivenUpEpisodes();
        Task<bool> SetFolderName(long id, string foldername);
        Task<List<FileInfo>> Dirlist(string dirName, int daysOldToAllow, string filter = "*.*", int minSizeAllowed = 50000);
        Task<List<TouchFile>> ShowDownloaded(int year = 0);
        Task<NzBplanetJSON> NZBPlanetSearch(Show show);
        Task TVSiteUpdate(int id, bool active, int order, string name, string urltemplate, 
            string apiKey = "", string apiBaseUrl = "", string rssApiKey = "", string rssBaseUrl = "");
        Task TVSiteDelete(int id);
        List<TVDirectories> TvDirectories();
        Task TVDirectoryUpdate(int id, string name, int daysToScan, string filter, int minFileSize, bool aliasable = false);
        Task TVDirectoryDelete(int id);
        Task CheckNewSeasonNotifications();

        // Feature 2: Statistics
        StatisticsModel GetStatistics();

        // Feature 3: Search improvements
        Task<List<TVMazeSearchResult>> SearchTvMaze(string query);
        (List<Show> results, int totalCount) AdvancedSearch(string? name, int? genreId, int? networkId, int? year,
            string? status = null, int? typeId = null, int? webNetworkId = null,
            int? languageId = null, int? countryId = null, string? wanted = null,
            int page = 1, int pageSize = 50);

        // Feature 4: Bulk actions
        Task BulkSetShowFilter(List<long> showIds, bool? state);
        Task CatchUpShow(long showId);
        Task GiveUpShow(long showId);

        // Feature 6: Download progress
        List<DownloadProgressModel> GetDownloadProgress();

        // Feature 10: Export/Import
        string ExportUserDataAsJson();
        Task<int> ImportUserDataFromJson(string json);

        // Show notes and priority
        Task SetShowNotes(long showId, string notes);
        Task SetShowPriority(long showId, int priority);

        // Episode counts for show cards
        Dictionary<int, (int watched, int total)> GetEpisodeCountsForShows(List<int> showIds);

        // Tonight's episodes
        List<EpFilter> TonightsEpisodes();

        // Similar shows
        List<Show> GetSimilarShows(long showId, int max = 5);

        // Duplicate detection
        List<Show> FindDuplicateShows();

        // Trending
        Task<List<TrendingShowModel>> GetTrendingShows();

        // Show comparison
        ShowComparisonModel CompareShows(long showId1, long showId2);

        // CSV export
        string ExportUserDataAsCsv();

        // Storage dashboard
        StorageDashboardModel GetStorageDashboard();

        // Import watched shows from file path list
        ImportPathsPreview PreviewImportWatchedFromPaths(string fileContent);
        Task<(int showsMatched, int episodesMarked)> CommitImportWatchedFromPaths(string fileContent);

        // Dedupe: find duplicate episode files across TV directories
        List<DuplicateFileEntry> FindDuplicateEpisodeFiles();
        bool DeleteFile(string filePath);

        // NZB site crawling for unwatched episodes
        Task<NzbSiteCrawlSummary> CrawlNzbSitesForShow(long showId);

        // NZB RSS feed crawling - uses RSS API keys, skips sites without RSS key configured
        Task<NzbSiteCrawlSummary> CrawlNzbRssFeedsForShow(long showId);

        // Existing folder detection for undecided shows
        List<ExistingFolderMatch> FindExistingFolders(Show show, List<ShowFolderAlias> aliases);

        // Show folder aliases
        List<ShowFolderAlias> GetFolderAliases(long showId);
        Task AddFolderAlias(long showId, string aliasName, int seasonOffset = 0);
        Task RemoveFolderAlias(int aliasId);

        // Friends management
        List<Friend> GetFriends();
        Task<Friend> AddFriend(string name, string email, string folderPath);
        Task UpdateFriend(int id, string name, string email, string folderPath);
        Task DeleteFriend(int id);
        Task AddFriendShow(int friendId, int showId);
        Task RemoveFriendShow(int friendShowId);
        List<Show> GetWatchedShows();
        List<FriendCopy> GetRecentCopiesForFriend(int friendId, int count = 10);

        // Show predecessor/successor links
        List<ShowLink> GetShowLinks(long showId);
        Task AddShowLink(long predecessorShowId, long successorShowId);
        Task RemoveShowLink(int showLinkId);
    }
}
