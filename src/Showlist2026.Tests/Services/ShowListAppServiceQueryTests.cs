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
        // UndecidedShows()'s "attach latest episode" step uses a raw SQL query, which needs a
        // real relational engine that also translates DateTimeOffset range comparisons -
        // neither fake provider available for testing offers both (see TestDb remarks), so
        // that branch only runs when there's at least one eligible result. This test instead
        // verifies the eligibility computation itself: every show here is excluded for a
        // different reason, so the raw-SQL branch is never reached and the result must be empty.
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
}
