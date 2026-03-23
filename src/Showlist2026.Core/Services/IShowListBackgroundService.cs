using System.Threading.Tasks;
using Showlist2026.Entities;
using Showlist2026.Models;

namespace Showlist2026.Services
{
    public interface IShowListBackgroundService
    {
        HomePageStats HomePageStats();
        Task<bool> RefreshNetworks();
        Task<bool> RefreshWebNetworks();
        Task<bool> RefreshShowBatch();
        Task<bool> RefreshShowEpisodes(Show show);
        Task<bool> RefreshShowPage(int pageno);
        Task<bool> RefreshShowPage(int pagenofrom, int pagenoto);
        Task<bool> RefreshShows();
        Task<bool> RefreshShowDates();
        Task<bool> PopulateShowFolderNames();
        Task<bool> BacklogPage();
        Task<bool> Notificationtest();
        int GetEstimatedPageMax();
        Task<bool> ShowDownloadedJob();
        Task<bool> ScanDirectoryFull(string directory);
    }
}
