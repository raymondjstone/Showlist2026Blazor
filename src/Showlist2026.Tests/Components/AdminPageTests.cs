using Bunit;
using System.Linq;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class AdminPageTests : BlazorTestBase
{
    public AdminPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void TestAllNotifications_SendsThroughRealBackgroundService()
    {
        var cut = Render<Admin>();

        cut.Find("button.btn-info").Click();

        Assert.Contains("Test Notifications completed successfully", cut.Markup);
        Assert.Single(Notifications.Sent);
    }

    [Fact]
    public void TestPushover_ReportsSuccessFromNotificationService()
    {
        var cut = Render<Admin>();

        cut.Find("button.btn-outline-info").Click();

        Assert.Contains("Pushover test sent successfully", cut.Markup);
    }

    [Fact]
    public void FindDuplicateShows_WithNoDuplicates_ReportsNoneFound()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad"));
            ctx.SaveChanges();
        }

        var cut = Render<Admin>();
        var findDuplicatesButton = cut.FindAll("button.btn-warning")
            .First(b => b.TextContent.Contains("Find Duplicate Shows"));
        findDuplicatesButton.Click();

        Assert.Contains("No duplicates found", cut.Markup);
    }

    [Fact]
    public void FullDirectoryScan_WithMissingDirectory_ReportsFailure()
    {
        var cut = Render<Admin>();

        var directoryInput = cut.FindAll("input.form-control")
            .First(i => (i.GetAttribute("placeholder") ?? "").StartsWith("e.g."));
        directoryInput.Change(@"D:\DoesNotExist_Showlist2026Test");

        var scanButton = cut.FindAll("button.btn-warning")
            .First(b => b.TextContent.Contains("Scan Directory"));
        scanButton.Click();

        Assert.Contains("Full Directory Scan failed", cut.Markup);
    }
}
