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

    [Fact]
    public void FiltersShow_DeletingAFilter_ReturnsShowToUndecided_AndDropsItFromTheList()
    {
        int wantedId, excludedId;
        using (var ctx = Db.CreateContext())
        {
            var wanted = TestData.NewShow("Wanted Show", wanted: true);
            var excluded = TestData.NewShow("Excluded Show", wanted: false);
            ctx.Shows.AddRange(wanted, excluded);
            ctx.SaveChanges();
            wantedId = wanted.Id;
            excludedId = excluded.Id;
        }

        var cut = Render<FiltersShow>();

        cut.Find("button[title='Click to remove the always included filter']").Click();
        cut.Find("button[title='Click to remove the always excluded filter']").Click();

        using var verify = Db.CreateContext();
        Assert.Null(verify.Shows.Find(wantedId)!.Wanted);
        Assert.Null(verify.Shows.Find(excludedId)!.Wanted);
        // Undecided shows never appear on this page (per its own doc comment), so both rows
        // should now be gone.
        Assert.DoesNotContain("Wanted Show", cut.Markup);
        Assert.DoesNotContain("Excluded Show", cut.Markup);
    }

    // The Genre/Type/Language/Network/WebNetwork/Country filter pages share an identical
    // 3-branch structure (undecided / always-included / always-excluded), each with its own
    // physical source lines for the Include/Exclude/Delete buttons. Cycling one item through
    // every state below exercises every button in every branch.

    [Fact]
    public void FiltersType_CyclingThroughAllStates_ExercisesEveryBranch()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var type = TestData.NewType("Scripted");
            var blank = TestData.NewType("");
            ctx.Types.AddRange(type, blank);
            ctx.SaveChanges();
            id = type.Id;
        }

        var cut = Render<FiltersType>();
        Assert.Contains("{No type specified}", cut.Markup);

        const string includeTitle = "Always include show if the type is Scripted";
        const string excludeTitle = "Always exclude show if the type is Scripted";

        cut.Find($"button[title='{excludeTitle}']").Click(); // undecided -> excluded
        cut.Find($"button[title='{includeTitle}']").Click(); // excluded -> included
        cut.Find($"button[title='{excludeTitle}']").Click(); // included -> excluded
        cut.Find("button[title='Click to remove the always excluded filter']").Click(); // excluded -> undecided
        cut.Find($"button[title='{includeTitle}']").Click(); // undecided -> included
        cut.Find("button[title='Click to remove the always included filter']").Click(); // included -> undecided

        using var verify = Db.CreateContext();
        Assert.Null(verify.Types.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersGenre_CyclingThroughAllStates_ExercisesEveryBranch()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var genre = TestData.NewGenreText("Drama");
            var blank = TestData.NewGenreText("");
            ctx.GenreTexts.AddRange(genre, blank);
            ctx.SaveChanges();
            id = genre.Id;
        }

        var cut = Render<FiltersGenre>();
        Assert.Contains("{No genre specified}", cut.Markup);

        const string includeTitle = "Always include show if the genre is Drama";
        const string excludeTitle = "Always exclude show if the genre is Drama";

        cut.Find($"button[title='{excludeTitle}']").Click();
        cut.Find($"button[title='{includeTitle}']").Click();
        cut.Find($"button[title='{excludeTitle}']").Click();
        cut.Find("button[title='Click to remove the always excluded filter']").Click();
        cut.Find($"button[title='{includeTitle}']").Click();
        cut.Find("button[title='Click to remove the always included filter']").Click();

        using var verify = Db.CreateContext();
        Assert.Null(verify.GenreTexts.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersLanguage_CyclingThroughAllStates_ExercisesEveryBranch()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var lang = TestData.NewLanguage("English");
            var blank = TestData.NewLanguage("");
            ctx.Languages.AddRange(lang, blank);
            ctx.SaveChanges();
            id = lang.Id;
        }

        var cut = Render<FiltersLanguage>();
        Assert.Contains("{No Language specified}", cut.Markup);

        const string includeTitle = "Always include show if the language is English";
        const string excludeTitle = "Always exclude show if the language is English";

        cut.Find($"button[title='{excludeTitle}']").Click();
        cut.Find($"button[title='{includeTitle}']").Click();
        cut.Find($"button[title='{excludeTitle}']").Click();
        cut.Find("button[title='Click to remove the always excluded filter']").Click();
        cut.Find($"button[title='{includeTitle}']").Click();
        cut.Find("button[title='Click to remove the always included filter']").Click();

        using var verify = Db.CreateContext();
        Assert.Null(verify.Languages.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersNetwork_CyclingThroughAllStates_ExercisesEveryBranch()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var network = TestData.NewNetwork("AMC", country: TestData.NewCountry("US"));
            var blank = TestData.NewNetwork("");
            ctx.Networks.AddRange(network, blank);
            ctx.SaveChanges();
            id = network.Id;
        }

        var cut = Render<FiltersNetwork>();
        Assert.Contains("{No name specified}", cut.Markup);
        Assert.Contains("(US)", cut.Markup);

        const string includeTitle = "Always include show if the network is AMC";
        const string excludeTitle = "Always exclude show if the network is AMC";

        cut.Find($"button[title='{excludeTitle}']").Click();
        cut.Find($"button[title='{includeTitle}']").Click();
        cut.Find($"button[title='{excludeTitle}']").Click();
        cut.Find("button[title='Click to remove the always excluded filter']").Click();
        cut.Find($"button[title='{includeTitle}']").Click();
        cut.Find("button[title='Click to remove the always included filter']").Click();

        using var verify = Db.CreateContext();
        Assert.Null(verify.Networks.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersWebNetwork_CyclingThroughAllStates_ExercisesEveryBranch()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var webNetwork = TestData.NewWebNetwork("Netflix", country: TestData.NewCountry("US"));
            var blank = TestData.NewWebNetwork("");
            ctx.WebNetworks.AddRange(webNetwork, blank);
            ctx.SaveChanges();
            id = webNetwork.Id;
        }

        var cut = Render<FiltersWebNetwork>();
        Assert.Contains("{No name specified}", cut.Markup);
        Assert.Contains("(US)", cut.Markup);

        const string includeTitle = "Always include show if the web network is Netflix";
        const string excludeTitle = "Always exclude show if the web network is Netflix";

        cut.Find($"button[title='{excludeTitle}']").Click();
        cut.Find($"button[title='{includeTitle}']").Click();
        cut.Find($"button[title='{excludeTitle}']").Click();
        cut.Find("button[title='Click to remove the always excluded filter']").Click();
        cut.Find($"button[title='{includeTitle}']").Click();
        cut.Find("button[title='Click to remove the always included filter']").Click();

        using var verify = Db.CreateContext();
        Assert.Null(verify.WebNetworks.Find(id)!.Wanted);
    }

    [Fact]
    public void FiltersCountry_CyclingThroughAllStates_ExercisesEveryBranch()
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

        const string includeTitle = "Always include show if the country is US";
        const string excludeTitle = "Always exclude show if the country is US";

        cut.Find($"button[title='{includeTitle}']").Click(); // undecided -> included
        cut.Find($"button[title='{excludeTitle}']").Click(); // included -> excluded
        cut.Find($"button[title='{includeTitle}']").Click(); // excluded -> included
        cut.Find("button[title='Click to remove the always included filter']").Click(); // included -> undecided
        cut.Find($"button[title='{excludeTitle}']").Click(); // undecided -> excluded
        cut.Find("button[title='Click to remove the always excluded filter']").Click(); // excluded -> undecided

        using var verify = Db.CreateContext();
        Assert.Null(verify.Countrys.Find(id)!.Wanted);
    }
}
