using Flurl.Http.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Showlist2026.Configuration;
using Showlist2026.Web.Services;
using Xunit;

namespace Showlist2026.Tests.Services;

public class NotificationServiceTests
{
    private static NotificationService Make(NotificationOptions options) =>
        new(Options.Create(options), NullLogger<NotificationService>.Instance);

    [Fact]
    public async Task SendAsync_IsNoOp_WhenAllChannelsDisabled()
    {
        using var httpTest = new HttpTest();
        var service = Make(new NotificationOptions());

        await service.SendAsync("Title", "Message");

        httpTest.ShouldNotHaveMadeACall();
    }

    [Fact]
    public async Task TestPushoverAsync_ReturnsNotEnabled_WhenDisabled()
    {
        var service = Make(new NotificationOptions());
        var (success, error) = await service.TestPushoverAsync();

        Assert.False(success);
        Assert.Contains("not enabled", error);
    }

    [Fact]
    public async Task TestDiscordAsync_ReturnsNotEnabled_WhenDisabled()
    {
        var service = Make(new NotificationOptions());
        var (success, error) = await service.TestDiscordAsync();

        Assert.False(success);
        Assert.Contains("not enabled", error);
    }

    [Fact]
    public async Task TestEmailAsync_ReturnsNotEnabled_WhenDisabled()
    {
        var service = Make(new NotificationOptions());
        var (success, error) = await service.TestEmailAsync();

        Assert.False(success);
        Assert.Contains("not enabled", error);
    }

    [Fact]
    public async Task TestDiscordAsync_PostsWebhookAndReturnsSuccess_WhenEnabled()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("ok");

        var service = Make(new NotificationOptions
        {
            Discord = new DiscordSettings { Enabled = true, WebhookUrl = "https://discord.example/webhook" }
        });

        var (success, error) = await service.TestDiscordAsync();

        Assert.True(success);
        Assert.Null(error);
        httpTest.ShouldHaveCalled("https://discord.example/webhook").WithVerb(HttpMethod.Post);
    }

    [Fact]
    public async Task SendAsync_SwallowsDiscordFailure_AndDoesNotThrow()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("error", 500);

        var service = Make(new NotificationOptions
        {
            Discord = new DiscordSettings { Enabled = true, WebhookUrl = "https://discord.example/webhook" }
        });

        // SendAsync (unlike TestDiscordAsync) has no outer try/catch of its own - the failure
        // must be swallowed inside SendDiscordAsync itself, or this throws and fails the test.
        await service.SendAsync("Title", "Message");
    }
}
