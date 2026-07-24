using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListAppServiceQueryTests
{
    [Fact]
    public async Task AiringAroundNow_NewShowDiscovery_DoesNotLeakBeyondRequestedWindow()
    {
        // Regression test: the S01E01 "new show discovery" query used to hard-code a flat
        // 90-day look-back regardless of the caller's window, so a narrow window like
        // TonightsEpisodes() (daysminus=0, daysplus=0) would surface premieres from up to
        // 90 days ago instead of just "tonight". The fix caps the look-back at the caller's
        // own `min` when that's narrower than 90 days.
        using var db = new TestDb();
        await using (var ctx = db.CreateContext())
        {
            var inWindow = TestData.NewShow("New Show In Window"); // undecided
            TestData.NewEpisode(inWindow, 1, 1, DateTimeOffset.UtcNow);

            var outOfWindow = TestData.NewShow("New Show 10 Days Ago"); // undecided
            TestData.NewEpisode(outOfWindow, 1, 1, DateTimeOffset.UtcNow.AddDays(-10));

            ctx.Shows.AddRange(inWindow, outOfWindow);
            await ctx.SaveChangesAsync();
        }

        var service = TestFactory.CreateAppService(db);
        // A narrow +/-1 day window: the old bug would still reach back 90 days for new-show
        // discovery and incorrectly include the show that premiered 10 days ago.
        var results = service.AiringAroundNowForUser(daysminus: -1, daysplus: 1);

        Assert.Contains(results, r => r.ep.show!.name == "New Show In Window");
        Assert.DoesNotContain(results, r => r.ep.show!.name == "New Show 10 Days Ago");
    }

    [Fact]
    public void AiringAroundNow_IncludesWantedShowEpisodesRegardlessOfEpisodeNumber()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Wanted Show", wanted: true);
            TestData.NewEpisode(show, 3, 7, DateTimeOffset.UtcNow); // not a premiere
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.AiringAroundNowForUser(daysminus: -1, daysplus: 1);

        Assert.Contains(results, r => r.ep.show!.name == "Wanted Show" && r.ep.season == 3 && r.ep.number == 7);
    }

    [Fact]
    public void AiringAroundNow_ExcludesIgnoredShow_UnlessIncludeIgnoredRequested()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Ignored Show", wanted: false);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);

        var withoutIgnored = service.AiringAroundNowForUser(daysminus: -1, daysplus: 1, includeIgnored: false);
        Assert.DoesNotContain(withoutIgnored, r => r.ep.show!.name == "Ignored Show");

        var withIgnored = service.AiringAroundNowForUser(daysminus: -1, daysplus: 1, includeIgnored: true);
        Assert.Contains(withIgnored, r => r.ep.show!.name == "Ignored Show");
    }

    [Fact]
    public void AiringAroundNow_FirstShowOnly_FiltersToSeasonOneEpisodeOne()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Wanted Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow);
            TestData.NewEpisode(show, 2, 3, DateTimeOffset.UtcNow);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.AiringAroundNowForUser(daysminus: -1, daysplus: 1, firstshowOnly: true);

        Assert.Single(results);
        Assert.Equal(1, results[0].ep.season);
        Assert.Equal(1, results[0].ep.number);
    }

    [Fact]
    public void AiringAroundNow_ExcludesWatchedEpisodes_UnlessIncludeWatchedRequested()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Wanted Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow, watched: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);

        var withoutWatched = service.AiringAroundNowForUser(daysminus: -1, daysplus: 1);
        Assert.Empty(withoutWatched);

        var withWatched = service.AiringAroundNowForUser(daysminus: -1, daysplus: 1, includeWatched: true);
        Assert.Single(withWatched);
    }

    [Fact]
    public void TonightsEpisodes_ExcludesDistantEpisodes()
    {
        // TonightsEpisodes() hard-codes AiringAroundNowForUser(0, 0). We can't assert exact
        // instant inclusion without a timing race, but a 30-day-old episode must never appear
        // in a same-day window regardless of the few milliseconds between setup and the call.
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Wanted Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.TonightsEpisodes();

        Assert.DoesNotContain(results, r => r.ep.show!.name == "Wanted Show");
    }

    [Fact]
    public void UndecidedShows_ExcludesAlreadyDecidedAndFilteredOutShows()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var decidedWanted = TestData.NewShow("Already Wanted", wanted: true);
            TestData.NewEpisode(decidedWanted, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));

            var decidedExcluded = TestData.NewShow("Already Excluded", wanted: false);
            TestData.NewEpisode(decidedExcluded, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));

            var excludedNetwork = TestData.NewNetwork("Bad Network", wanted: false);
            var networkFiltered = TestData.NewShow("Network Filtered", network: excludedNetwork);
            TestData.NewEpisode(networkFiltered, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));

            ctx.Networks.Add(excludedNetwork);
            ctx.Shows.AddRange(decidedWanted, decidedExcluded, networkFiltered);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.UndecidedShows();

        Assert.Empty(results);
    }

    [Fact]
    public void UndecidedShows_ReturnsEligibleShow_WithFirstAndLastEpisodeAttached()
    {
        // Regression coverage: UndecidedShows() used to load its "latest episode per show" via
        // FromSqlRaw, which the InMemory test provider can't execute at all - this whole path
        // (the actual point of the method) was untestable. Rewritten to two portable LINQ
        // queries (max-id-per-group, then load those rows), which InMemory runs the same as any
        // real relational provider.
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Undecided Show"); // no wanted decision
            var first = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30));
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-1)); // latest aired episode

            var decided = TestData.NewShow("Decided Show", wanted: true); // shouldn't appear
            TestData.NewEpisode(decided, 1, 1, DateTimeOffset.UtcNow.AddDays(-1));

            ctx.Shows.AddRange(show, decided);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.UndecidedShows();

        var result = Assert.Single(results);
        Assert.Equal("Undecided Show", result.ep.show!.name);
        Assert.Equal(1, result.ep.number); // S01E01 is the "discovery" episode returned as ep

        // Both the first (S01E01, same as `ep`) and the latest-aired (S01E02) episode should be
        // attached to show.Episodes for the card's first/last display.
        Assert.Equal(2, result.ep.show.Episodes!.Count);
        Assert.Contains(result.ep.show.Episodes, e => e.number == 2);
    }

    [Fact]
    public void MissedEpisodes_ReturnsOnlyAiredUnwatchedNotGivenUpForWantedShows()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var wanted = TestData.NewShow("Wanted", wanted: true);
            TestData.NewEpisode(wanted, 1, 1, DateTimeOffset.UtcNow.AddDays(-5)); // missed
            TestData.NewEpisode(wanted, 1, 2, DateTimeOffset.UtcNow.AddDays(-5), watched: true); // watched, not missed
            TestData.NewEpisode(wanted, 1, 3, DateTimeOffset.UtcNow.AddDays(-5), givenUp: true); // given up, not missed
            TestData.NewEpisode(wanted, 1, 4, DateTimeOffset.UtcNow.AddDays(5)); // not aired yet

            var notWanted = TestData.NewShow("Not Wanted", wanted: false);
            TestData.NewEpisode(notWanted, 1, 1, DateTimeOffset.UtcNow.AddDays(-5));

            ctx.Shows.AddRange(wanted, notWanted);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.MissedEpisodes();

        Assert.Single(results);
        Assert.Equal(1, results[0].ep.number);
        Assert.Equal("Wanted", results[0].ep.show!.name);
    }

    [Fact]
    public void MissedEpisodes_ReturnsEmpty_WhenNoShowsAreWanted()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var notWanted = TestData.NewShow("Not Wanted", wanted: false);
            TestData.NewEpisode(notWanted, 1, 1, DateTimeOffset.UtcNow.AddDays(-5));
            ctx.Shows.Add(notWanted);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);

        Assert.Empty(service.MissedEpisodes());
    }

    [Fact]
    public void NextUnwatchedPerShow_ReturnsEarliestUnwatchedAndCorrectCounts()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show", wanted: true, priority: 5);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10), watched: true);
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-9)); // earliest unwatched
            TestData.NewEpisode(show, 1, 3, DateTimeOffset.UtcNow.AddDays(-8), givenUp: true);
            TestData.NewEpisode(show, 1, 4, DateTimeOffset.UtcNow.AddDays(10)); // unaired
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.NextUnwatchedPerShow();

        var result = Assert.Single(results);
        Assert.Equal(2, result.ep.number);
        Assert.Equal(2, result.EpisodesBehind); // ep 2 (unwatched) + ep 3 (given up, still "behind")
        Assert.Equal(3, result.TotalAiredEpisodes); // eps 1,2,3 have aired; ep 4 hasn't
        // TotalWatchedEpisodes is populated from a query counting (Watched || GivenUp) together
        // (existing production semantics) - so it's ep1 (watched) + ep3 (given up) = 2.
        Assert.Equal(2, result.TotalWatchedEpisodes);
        Assert.Equal(1, result.TotalGivenUpEpisodes);
        Assert.Equal(5, result.ShowPriority);
    }

    [Fact]
    public void AiringAroundNow_SetsPerFieldDecisionFlags_ForShowsDecidedThroughEachFilterDimension()
    {
        // Each of these shows is undecided at the show level (Wanted == null), but becomes
        // "relevant" (and its per-field decision flags set) purely because a *different* entity
        // (type/network/webnetwork/language/genre) was independently marked wanted.
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var type = TestData.NewType("Scripted", wanted: true);
            var byType = TestData.NewShow("By Type", type: type);
            TestData.NewEpisode(byType, 1, 1, DateTimeOffset.UtcNow);

            var network = TestData.NewNetwork("AMC", wanted: true);
            var byNetwork = TestData.NewShow("By Network", network: network);
            TestData.NewEpisode(byNetwork, 1, 1, DateTimeOffset.UtcNow);

            var webNetwork = TestData.NewWebNetwork("Netflix", wanted: true);
            var byWebNetwork = TestData.NewShow("By WebNetwork", webNetwork: webNetwork);
            TestData.NewEpisode(byWebNetwork, 1, 1, DateTimeOffset.UtcNow);

            var language = TestData.NewLanguage("English", wanted: true);
            var byLanguage = TestData.NewShow("By Language", language: language);
            TestData.NewEpisode(byLanguage, 1, 1, DateTimeOffset.UtcNow);

            var genre = TestData.NewGenreText("Drama", wanted: true);
            var byGenre = TestData.NewShow("By Genre");
            byGenre.Genres = new List<Showlist2026.Entities.Genre> { new() { genretext = genre, show = byGenre } };
            TestData.NewEpisode(byGenre, 1, 1, DateTimeOffset.UtcNow);

            // WebNetworks.country decides it (Networks has no country at all), exercising the
            // "only set countryinclude via the web-network side" branch.
            var webCountry = new Showlist2026.Entities.Country { code = "GB", name = "GB", Wanted = true };
            var byWebCountry = TestData.NewShow("By WebCountry", webNetwork: TestData.NewWebNetwork("Hulu", country: webCountry));
            TestData.NewEpisode(byWebCountry, 1, 1, DateTimeOffset.UtcNow);

            ctx.Shows.AddRange(byType, byNetwork, byWebNetwork, byLanguage, byGenre, byWebCountry);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.AiringAroundNowForUser(-1, 1);

        var byTypeResult = results.Single(r => r.ep.show!.name == "By Type");
        Assert.True(byTypeResult.typeinclude);
        Assert.True(byTypeResult.Activelyselected);

        var byNetworkResult = results.Single(r => r.ep.show!.name == "By Network");
        Assert.True(byNetworkResult.networkinclude);

        var byWebNetworkResult = results.Single(r => r.ep.show!.name == "By WebNetwork");
        Assert.True(byWebNetworkResult.webnetworkinclude);

        var byLanguageResult = results.Single(r => r.ep.show!.name == "By Language");
        Assert.True(byLanguageResult.languageinclude);

        var byGenreResult = results.Single(r => r.ep.show!.name == "By Genre");
        Assert.True(byGenreResult.genreinclude);

        var byWebCountryResult = results.Single(r => r.ep.show!.name == "By WebCountry");
        Assert.True(byWebCountryResult.countryinclude);
    }
}
