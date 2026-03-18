using System.Threading.Tasks;

namespace Showlist2026.Services
{
    public interface INotificationService
    {
        Task SendAsync(string title, string message);
        Task<(bool success, string error)> TestPushoverAsync();
        Task<(bool success, string error)> TestDiscordAsync();
        Task<(bool success, string error)> TestEmailAsync();
    }
}
