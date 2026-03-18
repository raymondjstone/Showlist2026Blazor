using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Altairis.Pushover.Client;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Showlist2026.Configuration;
using Showlist2026.Services;

namespace Showlist2026.Web.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationOptions _options;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IOptions<NotificationOptions> options, ILogger<NotificationService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string title, string message)
        {
            if (_options.Pushover.Enabled)
            {
                await SendPushoverAsync(title, message);
            }

            if (_options.Discord.Enabled)
            {
                await SendDiscordAsync(title, message);
            }

            if (_options.Email.Enabled)
            {
                await SendEmailAsync(title, message);
            }
        }

        public async Task<(bool success, string error)> TestPushoverAsync()
        {
            if (!_options.Pushover.Enabled)
                return (false, "Pushover is not enabled in configuration.");
            try
            {
                await SendPushoverAsync("Showlist2026 Test", "Pushover integration is working.");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool success, string error)> TestDiscordAsync()
        {
            if (!_options.Discord.Enabled)
                return (false, "Discord is not enabled in configuration.");
            try
            {
                await SendDiscordAsync("Showlist2026 Test", "Discord integration is working.");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool success, string error)> TestEmailAsync()
        {
            if (!_options.Email.Enabled)
                return (false, "Email is not enabled in configuration.");
            try
            {
                await SendEmailAsync("Showlist2026 Test", "Email integration is working.");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task SendPushoverAsync(string title, string message)
        {
            try
            {
                var client = new PushoverClient(_options.Pushover.ApiKey);
                var msg = new PushoverMessage(_options.Pushover.UserKey, message)
                {
                    Title = title,
                    Sound = MessageSound.Magic
                };
                var result = await client.SendMessage(msg);
                if (!result.Status)
                {
                    _logger.LogWarning("Pushover message send failed for: {Title}", title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Pushover notification: {Title}", title);
            }
        }

        private async Task SendDiscordAsync(string title, string message)
        {
            try
            {
                await _options.Discord.WebhookUrl
                    .PostJsonAsync(new { content = $"**{title}**\n{message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Discord notification: {Title}", title);
            }
        }

        private async Task SendEmailAsync(string title, string message)
        {
            try
            {
                using var client = new SmtpClient(_options.Email.SmtpHost, _options.Email.SmtpPort)
                {
                    Credentials = new NetworkCredential(_options.Email.Username, _options.Email.Password),
                    EnableSsl = true
                };
                var mail = new MailMessage(_options.Email.From, _options.Email.To, title, message);
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification: {Title}", title);
            }
        }
    }
}
