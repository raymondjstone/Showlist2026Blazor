using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

/// <summary>
/// The Filters* reference-data pages (Country/Genre/Language/Type/WebNetwork) all follow the
/// identical load-list/set-filter/delete-filter pattern already proven end-to-end against
/// FiltersNetwork - one round-trip test each here confirms each page's specific AppService
/// method wiring (GenreData/GenreFilter, LanguageData/LanguageFilter, etc.) is correct.
/// </summary>
public class SimpleFilterPagesTests : BlazorTestBase
{
    [Fact]
    public void FiltersCountry_ClickingAlwaysExclude_PersistsThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var country = TestData.NewCountry("US");
            ctx.Countrys.Add(country);
            ctx.SaveChanges();
            id = country.Id;
        }

        var cut = Render<FiltersCountry>();
        Assert.Contains("US", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Always Exclude")).Click();

        using var verify = Db.CreateContext();
        Assert.False(verify.Countrys.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersGenre_ClickingAlwaysInclude_PersistsThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var genre = TestData.NewGenreText("Drama");
            ctx.GenreTexts.Add(genre);
            ctx.SaveChanges();
            id = genre.Id;
        }

        var cut = Render<FiltersGenre>();
        Assert.Contains("Drama", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Always Include")).Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.GenreTexts.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersLanguage_ClickingAlwaysInclude_PersistsThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var lang = TestData.NewLanguage("English");
            ctx.Languages.Add(lang);
            ctx.SaveChanges();
            id = lang.Id;
        }

        var cut = Render<FiltersLanguage>();
        Assert.Contains("English", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Always Include")).Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.Languages.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersType_ClickingAlwaysExclude_PersistsThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var type = TestData.NewType("Scripted");
            ctx.Types.Add(type);
            ctx.SaveChanges();
            id = type.Id;
        }

        var cut = Render<FiltersType>();
        Assert.Contains("Scripted", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Always Exclude")).Click();

        using var verify = Db.CreateContext();
        Assert.False(verify.Types.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersWebNetwork_ClickingAlwaysInclude_PersistsThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var webNetwork = TestData.NewWebNetwork("Netflix");
            ctx.WebNetworks.Add(webNetwork);
            ctx.SaveChanges();
            id = webNetwork.Id;
        }

        var cut = Render<FiltersWebNetwork>();
        Assert.Contains("Netflix", cut.Markup);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Always Include")).Click();

        using var verify = Db.CreateContext();
        Assert.True(verify.WebNetworks.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersShow_OnlyShowsDecidedShows_AndSearchFiltersByName()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad", wanted: true));
            ctx.Shows.Add(TestData.NewShow("Better Call Saul", wanted: false));
            ctx.Shows.Add(TestData.NewShow("Undecided Show"));
            ctx.SaveChanges();
        }

        var cut = Render<FiltersShow>();

        Assert.Contains("Breaking Bad", cut.Markup);
        Assert.Contains("Better Call Saul", cut.Markup);
        Assert.DoesNotContain("Undecided Show", cut.Markup);

        cut.Find("input.form-control").Input("Breaking");

        Assert.Contains("Breaking Bad", cut.Markup);
        Assert.DoesNotContain("Better Call Saul", cut.Markup);
    }

    [Fact]
    public void FiltersShow_ClickingAlwaysExclude_PersistsThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Wanted Show", wanted: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            id = show.Id;
        }

        var cut = Render<FiltersShow>();
        cut.FindAll("button").Single(b => b.TextContent.Contains("Always Exclude")).Click();

        using var verify = Db.CreateContext();
        Assert.False(verify.Shows.Find(id)!.Wanted);
    }
}
