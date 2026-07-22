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
}
