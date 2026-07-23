using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class FiltersNZBSitesPageTests : BlazorTestBase
{
    [Fact]
    public void AddingASite_PersistsThroughRealService()
    {
        var cut = Render<FiltersNZBSites>();

        cut.Find("input[placeholder='Name']").Change("MySite");
        cut.Find("input[placeholder='URL Template']").Change("http://example.com/{URLSearchTerm}");
        cut.Find("button.btn-success").Click();

        using var verify = Db.CreateContext();
        var site = Assert.Single(verify.TVSites);
        Assert.Equal("MySite", site.Name);
    }

    [Fact]
    public void UsingAnExample_PrefillsTheNewSiteFormWithoutSavingYet()
    {
        var cut = Render<FiltersNZBSites>();

        cut.Find("button.btn-outline-primary").Click(); // "Use" on the first example row

        Assert.Contains("Example loaded", cut.Markup);
        Assert.Equal("NZBGeek", cut.Find("input[placeholder='Name']").GetAttribute("value"));

        using var verify = Db.CreateContext();
        Assert.Empty(verify.TVSites); // not saved until "Add" is clicked
    }

    [Fact]
    public void DeletingASite_RemovesItThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var site = new Showlist2026.Entities.TVSite { Name = "Site", Order = 1, URLTemplate = "http://x" };
            ctx.TVSites.Add(site);
            ctx.SaveChanges();
            id = site.Id;
        }

        var cut = Render<FiltersNZBSites>();
        cut.Find("button.btn-danger").Click();

        using var verify = Db.CreateContext();
        Assert.Null(verify.TVSites.Find(id));
    }

    [Fact]
    public void EditingAnExistingSite_PersistsAllFieldsThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var site = new Showlist2026.Entities.TVSite
            {
                Name = "Old Name", Order = 1, URLTemplate = "http://old", Active = false
            };
            ctx.TVSites.Add(site);
            ctx.SaveChanges();
            id = site.Id;
        }

        var cut = Render<FiltersNZBSites>();

        Assert.Contains("No RSS", cut.Markup); // RSS key not set yet

        cut.Find("input.form-check-input").Change(true);
        cut.Find("input[placeholder='Order']").Change("5");
        cut.Find("input[placeholder='Name']").Change("New Name");
        cut.Find("input[placeholder='URL Template']").Change("http://new/{URLSearchTerm}");
        cut.Find("input[placeholder='(optional)']").Change("api-key-value");
        cut.Find("input[placeholder='Auto']").Change("https://api.example.com");
        cut.Find("input[placeholder='(for RSS crawl)']").Change("rss-key-value");
        cut.FindAll("input[placeholder='Auto']")[1].Change("https://rss.example.com");
        cut.Find("button.btn-primary.btn-sm").Click(); // Save

        Assert.Contains("Saved: New Name", cut.Markup);
        Assert.Contains("RSS Enabled", cut.Markup);

        using var verify = Db.CreateContext();
        var updated = verify.TVSites.Find(id)!;
        Assert.True(updated.Active);
        Assert.Equal(5, updated.Order);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("http://new/{URLSearchTerm}", updated.URLTemplate);
        Assert.Equal("api-key-value", updated.ApiKey);
        Assert.Equal("https://api.example.com", updated.ApiBaseUrl);
        Assert.Equal("rss-key-value", updated.RssApiKey);
        Assert.Equal("https://rss.example.com", updated.RssBaseUrl);
    }
}
