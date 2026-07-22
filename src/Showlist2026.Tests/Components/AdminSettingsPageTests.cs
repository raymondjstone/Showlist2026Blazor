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
}
