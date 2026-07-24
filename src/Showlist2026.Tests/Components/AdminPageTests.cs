using Bunit;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public void FindDuplicateShows_WithDuplicates_ListsThemWithTvMazeId()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad", showid: 99));
            ctx.Shows.Add(TestData.NewShow("Breaking Bad (dupe)", showid: 99));
            ctx.SaveChanges();
        }

        var cut = Render<Admin>();
        cut.FindAll("button.btn-warning").First(b => b.TextContent.Contains("Find Duplicate Shows")).Click();

        Assert.Contains("2 potential duplicates", cut.Markup);
        Assert.Contains("TVMaze: 99", cut.Markup);
    }

    [Fact]
    public void DismissAlert_ClearsStatusMessage()
    {
        var cut = Render<Admin>();
        cut.Find("button.btn-info").Click();
        Assert.Contains("Test Notifications completed successfully", cut.Markup);

        cut.Find("button.btn-close").Click();

        Assert.DoesNotContain("Test Notifications completed successfully", cut.Markup);
    }

    [Fact]
    public void BacklogPage_WithNoShowNeedingUpdate_ReportsFailure()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            show.needsupdate = false;
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<Admin>();
        cut.FindAll("button.btn-warning").First(b => b.TextContent.Contains("Backlog Page")).Click();

        Assert.Contains("Backlog Page completed successfully", cut.Markup);
        Assert.Contains("alert-danger", cut.Markup);
    }

    [Fact]
    public void PopulateShowFolderNames_WithNoMatchingDirectory_StillReportsSuccess()
    {
        var cut = Render<Admin>();
        cut.FindAll("button.btn-warning").First(b => b.TextContent.Contains("Populate Show Folder Names")).Click();

        Assert.Contains("Populate Show Folder Names completed successfully", cut.Markup);
        Assert.Contains("alert-success", cut.Markup);
    }

    [Fact]
    public void RefreshShowDates_Succeeds_ThroughRealBackgroundService()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new Dictionary<string, long>());

        var cut = Render<Admin>();
        cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Refresh Show Dates")).Click();

        Assert.Contains("Refresh Show Dates completed successfully", cut.Markup);
        Assert.Contains("alert-success", cut.Markup);
    }

    [Fact]
    public void RefreshShowPages_WithNoShowsInDatabase_ReportsFailure()
    {
        var cut = Render<Admin>();
        cut.FindAll("button.btn-warning").First(b => b.TextContent.Contains("Refresh Show Pages")).Click();

        Assert.Contains("Refresh Show Pages failed", cut.Markup);
    }

    [Fact]
    public void TestDiscord_ReportsSuccessFromNotificationService()
    {
        var cut = Render<Admin>();
        cut.FindAll("button.btn-outline-info").First(b => b.TextContent.Contains("Test Discord")).Click();

        Assert.Contains("Discord test sent successfully", cut.Markup);
    }

    [Fact]
    public void TestEmail_ReportsSuccessFromNotificationService()
    {
        var cut = Render<Admin>();
        cut.FindAll("button.btn-outline-info").First(b => b.TextContent.Contains("Test Email")).Click();

        Assert.Contains("Email test sent successfully", cut.Markup);
    }

    private sealed class ThrowingTestChannelNotificationService : Showlist2026.Services.INotificationService
    {
        public Task SendAsync(string title, string message) => Task.CompletedTask;
        public Task<(bool success, string error)> TestPushoverAsync() => throw new InvalidOperationException("pushover transport unavailable");
        public Task<(bool success, string error)> TestDiscordAsync() => Task.FromResult((true, ""));
        public Task<(bool success, string error)> TestEmailAsync() => Task.FromResult((true, ""));
    }

    [Fact]
    public void TestPushover_ReportsFailure_WhenNotificationServiceThrows()
    {
        Services.AddSingleton<Showlist2026.Services.INotificationService>(new ThrowingTestChannelNotificationService());

        var cut = Render<Admin>();
        cut.FindAll("button.btn-outline-info").First(b => b.TextContent.Contains("Test Pushover")).Click();

        Assert.Contains("Pushover test failed: pushover transport unavailable", cut.Markup);
    }

    [Fact]
    public void ExportData_InvokesJavaScriptDownloadAndReportsSuccess()
    {
        var cut = Render<Admin>();
        cut.FindAll("button.btn-success").First(b => b.TextContent.Contains("Export User Data (JSON)")).Click();

        Assert.Contains("Export downloaded.", cut.Markup);
        Assert.Single(JSInterop.Invocations, inv => inv.Identifier == "eval");
    }

    [Fact]
    public void ExportCsv_InvokesJavaScriptDownloadAndReportsSuccess()
    {
        var cut = Render<Admin>();
        cut.FindAll("button.btn-success").First(b => b.TextContent.Contains("Export User Data (CSV)")).Click();

        Assert.Contains("CSV export downloaded.", cut.Markup);
        Assert.Single(JSInterop.Invocations, inv => inv.Identifier == "eval");
    }

    [Fact]
    public void ExportData_ReportsFailure_WhenJavaScriptInvocationThrows()
    {
        JSInterop.SetupVoid("eval", _ => true).SetException(new InvalidOperationException("JS interop unavailable"));

        var cut = Render<Admin>();
        cut.FindAll("button.btn-success").First(b => b.TextContent.Contains("Export User Data (JSON)")).Click();

        Assert.Contains("Export failed:", cut.Markup);
    }

    [Fact]
    public void ExportCsv_ReportsFailure_WhenJavaScriptInvocationThrows()
    {
        JSInterop.SetupVoid("eval", _ => true).SetException(new InvalidOperationException("JS interop unavailable"));

        var cut = Render<Admin>();
        cut.FindAll("button.btn-success").First(b => b.TextContent.Contains("Export User Data (CSV)")).Click();

        Assert.Contains("CSV export failed:", cut.Markup);
    }

    [Fact]
    public void ImportData_ImportsExportedJson_ThroughRealAppService()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Show", showid: 555, wanted: true);
            TestData.NewEpisode(show, 1, 1, watched: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }
        var exportedJson = TestFactory.CreateAppService(Db).ExportUserDataAsJson();

        // Reset back to undecided so the import below has something to restore - import only
        // fills in shows/episodes that aren't already decided.
        using (var ctx = Db.CreateContext())
        {
            var show = ctx.Shows.Include(s => s.Episodes).Single(s => s.Id == showId);
            show.Wanted = null;
            foreach (var ep in show.Episodes!) ep.Watched = false;
            ctx.SaveChanges();
        }

        var cut = Render<Admin>();
        var fileInput = cut.FindComponents<InputFile>()[0];
        fileInput.UploadFiles(InputFileContent.CreateFromText(exportedJson, "export.json"));

        var importButton = cut.FindAll("button.btn-outline-primary").First(b => b.TextContent.Contains("Import"));
        importButton.Click();

        Assert.Contains("Import completed.", cut.Markup);
        using var verify = Db.CreateContext();
        Assert.True(verify.Shows.Find(showId)!.Wanted);
        Assert.True(verify.Episodes.Single(e => e.show!.Id == showId).Watched);
    }

    [Fact]
    public void ImportPaths_PreviewThenCommit_MarksShowWantedThroughRealAppService()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", folderName: "My.Show", premiered: "2010-01-01");
            TestData.NewEpisode(show, 1, 1);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }
        var fileContent = string.Join("\n", new[]
        {
            @"D:\TV\My.Show\Season 1\My.Show.S01E01.mkv",
            @"D:\TV\Unmatched.Show\Season 1\Unmatched.Show.S01E01.mkv",
        });

        var cut = Render<Admin>();
        var fileInput = cut.FindComponents<InputFile>()[1];
        fileInput.UploadFiles(InputFileContent.CreateFromText(fileContent, "paths.txt"));

        cut.FindAll("button.btn-outline-primary").First(b => b.TextContent.Contains("Preview Matches")).Click();

        Assert.Contains("1</strong> shows matched", cut.Markup);
        Assert.Contains("Unmatched.Show", cut.Markup);
        Assert.Contains("1 unmatched folders", cut.Markup);

        cut.FindAll("button.btn-success").First(b => b.TextContent.Contains("Confirm")).Click();

        Assert.Contains("Import complete", cut.Markup);
        using var verify = Db.CreateContext();
        Assert.True(verify.Shows.Find(showId)!.Wanted);
    }

    [Fact]
    public void RefreshNetworks_Succeeds_ThroughRealBackgroundService()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith(status: 404);
        httpTest.ForCallsTo("*/networks/1").RespondWithJson(new
        {
            id = 42,
            name = "AMC",
            country = new { name = "United States", code = "US", timezone = "America/New_York" }
        });

        var cut = Render<Admin>();
        cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Refresh Networks")).Click();

        Assert.Contains("Refresh Networks completed successfully", cut.Markup);
        Assert.Contains("alert-success", cut.Markup);
    }

    [Fact]
    public void RefreshShowBatch_Succeeds_ThroughRealBackgroundService()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Show", showid: 1));
            ctx.SaveChanges();
        }
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(System.Array.Empty<object>());

        var cut = Render<Admin>();
        cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Refresh Show Batch")).Click();

        Assert.Contains("Refresh Show Batch completed successfully", cut.Markup);
        Assert.Contains("alert-success", cut.Markup);
    }

    [Fact]
    public void RefreshShows_Succeeds_ThroughRealBackgroundService_WithNoShowsNeedingUpdate()
    {
        var cut = Render<Admin>();
        cut.FindAll("button.btn-primary").First(b => b.TextContent.Contains("Refresh Shows")).Click();

        Assert.Contains("Refresh Shows completed successfully", cut.Markup);
        Assert.Contains("alert-success", cut.Markup);
    }

    [Fact]
    public void RefreshShowPages_Succeeds_ThroughRealBackgroundService_WhenShowsExist()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Show", showid: 1));
            ctx.SaveChanges();
        }
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(System.Array.Empty<object>());

        var cut = Render<Admin>();
        cut.FindAll("button.btn-warning").First(b => b.TextContent.Contains("Refresh Show Pages")).Click();

        Assert.Contains("Refresh Show Pages completed successfully", cut.Markup);
        Assert.Contains("alert-success", cut.Markup);
    }

    [Fact]
    public void ImportData_WithMalformedJson_ReportsFailure()
    {
        var cut = Render<Admin>();
        var fileInput = cut.FindComponents<InputFile>()[0];
        fileInput.UploadFiles(InputFileContent.CreateFromText("not valid json {{{", "export.json"));

        var importButton = cut.FindAll("button.btn-outline-primary").First(b => b.TextContent.Contains("Import"));
        importButton.Click();

        Assert.Contains("Import failed", cut.Markup);
        Assert.Contains("alert-danger", cut.Markup);
    }
}
