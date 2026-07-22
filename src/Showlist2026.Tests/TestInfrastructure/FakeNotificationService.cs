using Showlist2026.Services;

namespace Showlist2026.Tests.TestInfrastructure;

/// <summary>Captures calls instead of sending real notifications (Pushover/Discord/Email).</summary>
public sealed class FakeNotificationService : INotificationService
{
    public List<(string Title, string Message)> Sent { get; } = new();

    public Task SendAsync(string title, string message)
    {
        Sent.Add((title, message));
        return Task.CompletedTask;
    }

    public Task<(bool success, string error)> TestPushoverAsync() => Task.FromResult((true, ""));
    public Task<(bool success, string error)> TestDiscordAsync() => Task.FromResult((true, ""));
    public Task<(bool success, string error)> TestEmailAsync() => Task.FromResult((true, ""));
}
