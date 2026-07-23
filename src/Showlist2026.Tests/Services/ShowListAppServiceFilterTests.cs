using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListAppServiceFilterTests
{
    [Fact]
    public async Task ShowFilter_SetsWantedState()
    {
        using var db = new TestDb();
        int showId;
        await using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Some Show");
            ctx.Shows.Add(show);
            await ctx.SaveChangesAsync();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var ok = await service.ShowFilter(showId, true);
        Assert.True(ok);

        await using var verify = db.CreateContext();
        Assert.True(verify.Shows.Find(showId)!.Wanted);
    }

    [Fact]
    public async Task ShowFilter_ReturnsFalse_WhenShowMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var ok = await service.ShowFilter(999, true);

        Assert.False(ok);
    }

    [Fact]
    public async Task WatchedFilter_SettingWatchedClearsGivenUp()
    {
        using var db = new TestDb();
        int epId;
        await using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            var ep = TestData.NewEpisode(show, 1, 1, givenUp: true);
            ctx.Shows.Add(show);
            await ctx.SaveChangesAsync();
            epId = ep.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var ok = await service.WatchedFilter(epId, true);
        Assert.True(ok);

        await using var verify = db.CreateContext();
        var ep2 = verify.Episodes.Find(epId)!;
        Assert.True(ep2.Watched);
        Assert.False(ep2.GivenUp);
    }

    [Fact]
    public async Task WatchedFilter_ReturnsFalse_WhenEpisodeMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        Assert.False(await service.WatchedFilter(999, true));
    }

    [Fact]
    public async Task GivenUpFilter_TogglesGivenUpFlag()
    {
        using var db = new TestDb();
        int epId;
        await using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            var ep = TestData.NewEpisode(show, 1, 1);
            ctx.Shows.Add(show);
            await ctx.SaveChangesAsync();
            epId = ep.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.GivenUpFilter(epId, true);

        await using var verify = db.CreateContext();
        Assert.True(verify.Episodes.Find(epId)!.GivenUp);
    }

    [Fact]
    public async Task SeasonWatchedFilter_MarksWholeSeasonWatched_AndClearsGivenUp()
    {
        // Regression test: previously this method dereferenced the show before checking
        // for null, and issued a per-episode SaveChanges. Also verifies the season filter
        // (season = 2) doesn't touch other seasons.
        using var db = new TestDb();
        int showId;
        await using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            TestData.NewEpisode(show, 2, 1, givenUp: true);
            TestData.NewEpisode(show, 2, 2);
            TestData.NewEpisode(show, 1, 1); // different season, must be untouched
            ctx.Shows.Add(show);
            await ctx.SaveChangesAsync();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var ok = await service.SeasonWatchedFilter(showId, 2, true);
        Assert.True(ok);

        await using var verify = db.CreateContext();
        var eps = verify.Episodes.Where(e => e.show!.Id == showId).ToList();
        Assert.All(eps.Where(e => e.season == 2), e => Assert.True(e.Watched));
        Assert.All(eps.Where(e => e.season == 2), e => Assert.False(e.GivenUp));
        Assert.False(eps.Single(e => e.season == 1).Watched);
    }

    [Fact]
    public async Task SeasonWatchedFilter_ReturnsFalse_WhenShowMissing_DoesNotThrow()
    {
        // Regression test for the fixed NullReferenceException: the show lookup used to be
        // dereferenced (s.Id) one line before the null check.
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var ok = await service.SeasonWatchedFilter(999, 1, true);

        Assert.False(ok);
    }

    [Fact]
    public async Task BulkSetShowFilter_UpdatesAllGivenIds()
    {
        using var db = new TestDb();
        int id1, id2, id3;
        await using (var ctx = db.CreateContext())
        {
            var s1 = TestData.NewShow("A");
            var s2 = TestData.NewShow("B");
            var s3 = TestData.NewShow("C");
            ctx.Shows.AddRange(s1, s2, s3);
            await ctx.SaveChangesAsync();
            (id1, id2, id3) = (s1.Id, s2.Id, s3.Id);
        }

        var service = TestFactory.CreateAppService(db);
        await service.BulkSetShowFilter(new List<long> { id1, id2 }, true);

        await using var verify = db.CreateContext();
        Assert.True(verify.Shows.Find(id1)!.Wanted);
        Assert.True(verify.Shows.Find(id2)!.Wanted);
        Assert.Null(verify.Shows.Find(id3)!.Wanted);
    }

    [Fact]
    public async Task CatchUpShow_MarksOnlyAiredUnwatchedEpisodesWatched()
    {
        using var db = new TestDb();
        int showId;
        await using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10)); // aired
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(10));  // not aired yet
            ctx.Shows.Add(show);
            await ctx.SaveChangesAsync();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.CatchUpShow(showId);

        await using var verify = db.CreateContext();
        var eps = verify.Episodes.Where(e => e.show!.Id == showId).ToList();
        Assert.True(eps.Single(e => e.number == 1).Watched);
        Assert.False(eps.Single(e => e.number == 2).Watched);
    }

    [Fact]
    public async Task GiveUpShow_MarksOnlyAiredUnwatchedEpisodesGivenUp()
    {
        using var db = new TestDb();
        int showId;
        await using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10));
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-5), watched: true);
            ctx.Shows.Add(show);
            await ctx.SaveChangesAsync();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.GiveUpShow(showId);

        await using var verify = db.CreateContext();
        var eps = verify.Episodes.Where(e => e.show!.Id == showId).ToList();
        Assert.True(eps.Single(e => e.number == 1).GivenUp);
        Assert.False(eps.Single(e => e.number == 2).GivenUp); // already watched, untouched
    }

    [Fact]
    public async Task LanguageFilter_ReturnsFalse_WhenLanguageMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        Assert.False(await service.LanguageFilter(999, true));
    }

    [Fact]
    public async Task TypeFilter_ReturnsFalse_WhenTypeMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        Assert.False(await service.TypeFilter(999, true));
    }

    [Fact]
    public async Task NetworkFilter_ReturnsFalse_WhenNetworkMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        Assert.False(await service.NetworkFilter(999, true));
    }

    [Fact]
    public async Task WebNetworkFilter_ReturnsFalse_WhenWebNetworkMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        Assert.False(await service.WebNetworkFilter(999, true));
    }

    [Fact]
    public async Task GenreFilter_ReturnsFalse_WhenGenreTextMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        Assert.False(await service.GenreFilter(999, true));
    }

    [Fact]
    public async Task CountryFilter_ReturnsFalse_WhenCountryMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        Assert.False(await service.CountryFilter(999, true));
    }

    [Fact]
    public async Task GivenUpFilter_ReturnsFalse_WhenEpisodeMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        Assert.False(await service.GivenUpFilter(999, true));
    }
}
