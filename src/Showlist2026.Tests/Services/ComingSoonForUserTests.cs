using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ComingSoonForUserTests
{
    [Fact]
    public void ComingSoonForUser_MatchesRealisticIsoFormattedPremieredDates()
    {
        // Regression test for a fixed bug: ComingSoonForUser used to pre-filter shows by
        // checking whether `premiered` CONTAINS "/<year>" (slash-prefixed), e.g.
        // .Contains("/2026") - but TVMaze's actual `premiered` field is ISO-formatted
        // ("2026-05-01"), which never contains a slash. That silently excluded every real show,
        // making the whole method return empty in production. Fixed to check for a leading
        // year instead (StartsWith), which matches the real ISO format.
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Upcoming Show", premiered: DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.ComingSoonForUser();

        Assert.Contains(results, r => r.ep.name == "Upcoming Show");
    }

    [Fact]
    public void ComingSoonForUser_AppliesWindowAndFilterLogic()
    {
        using var db = new TestDb();
        var premieredStr = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        using (var ctx = db.CreateContext())
        {
            var inWindow = TestData.NewShow("In Window", premiered: premieredStr);
            var excluded = TestData.NewShow("Excluded", wanted: false, premiered: premieredStr);
            var outsideWindow = TestData.NewShow("Outside Window", premiered: DateTime.UtcNow.AddYears(-5).ToString("yyyy-MM-dd"));
            ctx.Shows.AddRange(inWindow, excluded, outsideWindow);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.ComingSoonForUser();

        Assert.Contains(results, r => r.ep.name == "In Window");
        Assert.DoesNotContain(results, r => r.ep.name == "Excluded");
        Assert.DoesNotContain(results, r => r.ep.name == "Outside Window");
    }

    [Fact]
    public void ComingSoonForUser_ExcludesShowsDecidedThroughEachFilterDimension()
    {
        using var db = new TestDb();
        var premieredStr = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        using (var ctx = db.CreateContext())
        {
            var excludedType = TestData.NewType("Talk Show", wanted: false);
            var byType = TestData.NewShow("Excluded By Type", premiered: premieredStr, type: excludedType);

            var excludedNetwork = TestData.NewNetwork("Bad Network", wanted: false);
            var byNetwork = TestData.NewShow("Excluded By Network", premiered: premieredStr, network: excludedNetwork);

            var excludedWebNetwork = TestData.NewWebNetwork("Bad Web Network", wanted: false);
            var byWebNetwork = TestData.NewShow("Excluded By WebNetwork", premiered: premieredStr, webNetwork: excludedWebNetwork);

            var excludedLanguage = TestData.NewLanguage("Klingon", wanted: false);
            var byLanguage = TestData.NewShow("Excluded By Language", premiered: premieredStr, language: excludedLanguage);

            var excludedCountry = new Showlist2026.Entities.Country { code = "XX", name = "Excluded Land", Wanted = false };
            var byCountry = TestData.NewShow("Excluded By Country", premiered: premieredStr,
                network: TestData.NewNetwork("Some Network", country: excludedCountry));

            var excludedGenre = TestData.NewGenreText("Reality", wanted: false);
            var byGenre = TestData.NewShow("Excluded By Genre", premiered: premieredStr);
            byGenre.Genres = new List<Showlist2026.Entities.Genre> { new() { genretext = excludedGenre, show = byGenre } };

            var explicitlyWanted = TestData.NewShow("Explicitly Wanted", premiered: premieredStr, wanted: true);

            var undecided = TestData.NewShow("Still Undecided", premiered: premieredStr);

            ctx.Shows.AddRange(byType, byNetwork, byWebNetwork, byLanguage, byCountry, byGenre, explicitlyWanted, undecided);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.ComingSoonForUser();

        Assert.DoesNotContain(results, r => r.ep.name == "Excluded By Type");
        Assert.DoesNotContain(results, r => r.ep.name == "Excluded By Network");
        Assert.DoesNotContain(results, r => r.ep.name == "Excluded By WebNetwork");
        Assert.DoesNotContain(results, r => r.ep.name == "Excluded By Language");
        Assert.DoesNotContain(results, r => r.ep.name == "Excluded By Country");
        Assert.DoesNotContain(results, r => r.ep.name == "Excluded By Genre");
        Assert.DoesNotContain(results, r => r.ep.name == "Explicitly Wanted");
        Assert.Contains(results, r => r.ep.name == "Still Undecided");
    }

    [Fact]
    public void ComingSoonForUser_ExcludesShow_ByWebNetworkCountry_WhenShowHasNoMainNetwork()
    {
        // Networks.country and WebNetworks.country are each checked in turn - this covers the
        // WebNetworks.country branch specifically (no Networks assigned at all).
        using var db = new TestDb();
        var premieredStr = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        using (var ctx = db.CreateContext())
        {
            var excludedCountry = new Showlist2026.Entities.Country { code = "YY", name = "Excluded Web Land", Wanted = false };
            var byWebCountry = TestData.NewShow("Excluded By WebCountry", premiered: premieredStr,
                webNetwork: TestData.NewWebNetwork("Some Web Network", country: excludedCountry));
            ctx.Shows.Add(byWebCountry);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.ComingSoonForUser();

        Assert.DoesNotContain(results, r => r.ep.name == "Excluded By WebCountry");
    }
}
