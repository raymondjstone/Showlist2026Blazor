namespace Showlist2026.Configuration
{
    public class NotificationOptions
    {
        public PushoverSettings Pushover { get; set; } = new();
        public DiscordSettings Discord { get; set; } = new();
        public EmailSettings Email { get; set; } = new();
    }

    public class PushoverSettings
    {
        public bool Enabled { get; set; }
        public string ApiKey { get; set; } = "";
        public string UserKey { get; set; } = "";
    }

    public class DiscordSettings
    {
        public bool Enabled { get; set; }
        public string WebhookUrl { get; set; } = "";
    }

    public class EmailSettings
    {
        public bool Enabled { get; set; }
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}
