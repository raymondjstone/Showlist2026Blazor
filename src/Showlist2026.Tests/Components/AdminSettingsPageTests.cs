using Bunit;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Showlist2026.Data;
using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class AdminSettingsPageTests : BunitContext
{
    private readonly TestDb _db = new();

    public AdminSettingsPageTests()
    {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Services.AddSingleton(config);
        Services.AddSingleton<IDbContextFactory<ShowlistDbContext>>(new TestDbContextFactory(_db.Options));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _db.Dispose();
        base.Dispose(disposing);
    }

    [Fact]
    public void NoOverridesConfigured_RendersDefaultsForAllSettings()
    {
        var cut = Render<AdminSettings>();

        Assert.Contains("Showlist:TvNameListPath", cut.Markup);
        Assert.Contains("(not set)", cut.Markup);
    }

    [Fact]
    public void SavingASetting_PersistsAsDatabaseOverride()
    {
        var cut = Render<AdminSettings>();

        var row = cut.FindAll("tr").First(r => r.TextContent.Contains("Showlist:TvNameListPath"));
        row.QuerySelector("input.form-control-sm")!.Change(@"D:\CustomTvList\");

        row = cut.FindAll("tr").First(r => r.TextContent.Contains("Showlist:TvNameListPath"));
        row.QuerySelector("button.btn-outline-primary")!.Click();

        using var verify = _db.CreateContext();
        var setting = verify.AppSettings.Find("Showlist:TvNameListPath");
        Assert.NotNull(setting);
        Assert.Equal(@"D:\CustomTvList\", setting!.Value);
        Assert.Contains("Saved", cut.Markup);
        // TvNameListPath is not a sensitive setting, so its current value renders in the clear.
        Assert.Contains(@"D:\CustomTvList\", cut.Markup);
    }

    [Fact]
    public void DismissingTheStatusMessage_HidesTheAlert()
    {
        var cut = Render<AdminSettings>();

        var row = cut.FindAll("tr").First(r => r.TextContent.Contains("Showlist:TvNameListPath"));
        row.QuerySelector("input.form-control-sm")!.Change(@"D:\CustomTvList\");
        row = cut.FindAll("tr").First(r => r.TextContent.Contains("Showlist:TvNameListPath"));
        row.QuerySelector("button.btn-outline-primary")!.Click();
        Assert.Contains("Saved", cut.Markup);

        cut.Find("button.btn-close").Click();

        Assert.DoesNotContain("Saved", cut.Markup);
    }

    [Fact]
    public void LoadSettings_SwallowsAndFallsBackToNoOverrides_WhenDbHasCaseInsensitiveDuplicateKeys()
    {
        // AppSetting.Key is a case-sensitive primary key, so both rows save fine - but
        // LoadSettings loads them into a case-INSENSITIVE dictionary, which throws on
        // the duplicate. That must be caught rather than crashing the whole page.
        using (var ctx = _db.CreateContext())
        {
            ctx.AppSettings.Add(new AppSetting { Key = "Showlist:TvNameListPath", Value = "A" });
            ctx.AppSettings.Add(new AppSetting { Key = "showlist:tvnamelistpath", Value = "B" });
            ctx.SaveChanges();
        }

        var cut = Render<AdminSettings>();

        Assert.Contains("Showlist:TvNameListPath", cut.Markup);
        Assert.Contains("(not set)", cut.Markup); // DB overrides discarded, falls back to defaults
    }

    [Fact]
    public void DeletingADbOverride_RemovesItAndFallsBackToDefault()
    {
        using (var ctx = _db.CreateContext())
        {
            ctx.AppSettings.Add(new AppSetting { Key = "Showlist:TvNameListPath", Value = @"D:\Override\" });
            ctx.SaveChanges();
        }

        var cut = Render<AdminSettings>();
        var row = cut.FindAll("tr").First(r => r.TextContent.Contains("Showlist:TvNameListPath"));
        row.QuerySelector("button.btn-outline-danger")!.Click();

        using var verify = _db.CreateContext();
        Assert.Null(verify.AppSettings.Find("Showlist:TvNameListPath"));
        Assert.Contains("Removed DB override", cut.Markup);
    }

    [Fact]
    public void UpdatingAnExistingDbOverride_ModifiesTheSameRow()
    {
        using (var ctx = _db.CreateContext())
        {
            ctx.AppSettings.Add(new AppSetting { Key = "Showlist:TvNameListPath", Value = @"D:\Old\" });
            ctx.SaveChanges();
        }

        var cut = Render<AdminSettings>();
        var row = cut.FindAll("tr").First(r => r.TextContent.Contains("Showlist:TvNameListPath"));
        row.QuerySelector("input.form-control-sm")!.Change(@"D:\New\");

        row = cut.FindAll("tr").First(r => r.TextContent.Contains("Showlist:TvNameListPath"));
        row.QuerySelector("button.btn-outline-primary")!.Click();

        using var verify = _db.CreateContext();
        Assert.Single(verify.AppSettings.Where(s => s.Key == "Showlist:TvNameListPath"));
        Assert.Equal(@"D:\New\", verify.AppSettings.Find("Showlist:TvNameListPath")!.Value);
    }

    [Fact]
    public void SensitiveSetting_MasksItsValue_WhenOverridden()
    {
        // "Current Value" reflects IConfiguration directly (not the raw DB row - in production
        // DbConfigurationProvider layers DB overrides into IConfiguration itself), so set it via
        // config here to exercise the masking branch.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Showlist:NzbPlanetApiKey"] = "supersecretkey123"
            })
            .Build();
        Services.AddSingleton<IConfiguration>(config);

        var cut = Render<AdminSettings>();

        Assert.Contains("****y123", cut.Markup);
        Assert.DoesNotContain("supersecretkey123", cut.Markup);
    }

    [Fact]
    public void SettingSourcedFromEnvironmentVariable_ShowsEnvironmentBadge()
    {
        Environment.SetEnvironmentVariable("Showlist__TvMazeBaseUrl", "http://env-override.example");
        try
        {
            var cut = Render<AdminSettings>();

            var row = cut.FindAll("tr").First(r => r.TextContent.Contains("Showlist:TvMazeBaseUrl"));
            Assert.Contains("bg-info", row.InnerHtml);
            Assert.Contains("Environment", row.TextContent);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Showlist__TvMazeBaseUrl", null);
        }
    }

    [Fact]
    public void MasksPasswordInConnectionString()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=myserver;Database=mydb;Password=hunter2;User Id=sa;"
            })
            .Build();
        Services.AddSingleton<IConfiguration>(config);

        var cut = Render<AdminSettings>();

        Assert.Contains("Password=****", cut.Markup);
        Assert.DoesNotContain("hunter2", cut.Markup);
    }
}
