using Flurl.Http.Testing;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListBackgroundServiceHttpTests
{
    [Fact]
    public async Task RefreshWebNetworks_IsANoOpReturningTrue()
    {
        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.RefreshWebNetworks());
    }

    [Fact]
    public async Task RefreshShowEpisodes_AddsNewEpisodes_FromTvMazeResponse()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new
            {
                id = 1001,
                name = "Pilot",
                season = 1,
                number = 1,
                airdate = "2024-01-01",
                airtime = "20:00",
                runtime = 60,
                summary = "First episode",
                image = new { medium = "http://img/m.jpg", original = "http://img/o.jpg" },
                _links = new { self = new { href = "http://api/episodes/1001" } }
            }
        });

        using var db = new TestDb();
        var show = TestData.NewShow("My Show", showid: 555);
        using var ctx = db.CreateContext();
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx, TestFactory.Options());
        var result = await service.RefreshShowEpisodes(show);

        Assert.True(result);
        var ep = Assert.Single(show.Episodes!);
        Assert.Equal(1001, ep.episodeid);
        Assert.Equal("Pilot", ep.name);
        Assert.Equal(1, ep.season);
        Assert.Equal(1, ep.number);
        httpTest.ShouldHaveCalled("*/shows/555/episodes?specials=1");
    }

    [Fact]
    public async Task RefreshShowEpisodes_RemovesEpisodesNoLongerReturnedByTvMaze()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(Array.Empty<object>());

        using var db = new TestDb();
        var show = TestData.NewShow("My Show", showid: 555);
        TestData.NewEpisode(show, 1, 1, episodeid: 999);
        using var ctx = db.CreateContext();
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        var result = await service.RefreshShowEpisodes(show);

        Assert.True(result);
        Assert.Empty(show.Episodes!);
    }

    [Fact]
    public async Task RefreshShowEpisodes_ReturnsFalse_OnNonRetryableError()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("server error", 500);

        using var db = new TestDb();
        var show = TestData.NewShow("My Show", showid: 555);
        using var ctx = db.CreateContext();
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        var result = await service.RefreshShowEpisodes(show);

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshShowDates_MarksExistingShowNeedsUpdate_WhenTimestampChanges()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new Dictionary<string, long>
        {
            ["555"] = 1700000000, // existing show, timestamp differs -> needsupdate
            ["777"] = 1600000000, // brand new show id -> gets created
        });

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("My Show", showid: 555);
            show.updated = "1";
            show.needsupdate = false;
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            await service.RefreshShowDates();
        }

        using var verify = db.CreateContext();
        var existing = verify.Shows.Find(showId)!;
        Assert.True(existing.needsupdate);
        Assert.Equal("1700000000", existing.updated);

        var created = verify.Shows.Single(s => s.showid == 777);
        Assert.True(created.needsupdate);
    }

    [Fact]
    public async Task RefreshShowPage_CreatesNewShowWithEpisodes_FromTvMazeResponse()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new
            {
                id = 42,
                name = "Brand New Show",
                status = "Running",
                premiered = "2024-01-01",
                summary = "A new show",
                updated = 1700000000,
                url = "http://tvmaze/shows/42",
                weight = 0,
                network = new
                {
                    id = 9,
                    name = "HBO",
                    country = new { name = "United States", code = "US", timezone = "America/New_York" }
                },
                webChannel = new
                {
                    id = 11,
                    name = "HBO Max",
                    country = new { name = "United States", code = "US", timezone = "America/New_York" }
                },
                genres = new[] { "Drama" },
                type = "Scripted",
                language = "English",
                schedule = new { time = "21:00", days = new[] { "Sunday" } },
                rating = new { average = 8.5 },
                image = new { medium = "http://img/m.jpg", original = "http://img/o.jpg" },
                externals = new { tvrage = 12345, thetvdb = 67890, imdb = "tt1234567" },
                _links = new { self = new { href = "http://tvmaze/shows/42" } },
            }
        });
        httpTest.RespondWithJson(Array.Empty<object>()); // episodes fetch for the new show

        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        var result = await service.RefreshShowPage(0, 0);

        Assert.True(result);
        var show = ctx.Shows.Single(s => s.showid == 42);
        Assert.Equal("Brand New Show", show.name);
        Assert.Equal("Running", show.status);
        Assert.Equal("HBO Max", show.WebNetworks!.name);
        Assert.Equal("12345", show.tvrage);
        Assert.Equal("67890", show.thetvdb);
        Assert.Equal("tt1234567", show.imdb);
        Assert.Equal("http://img/m.jpg", show.imagemed);
        Assert.Equal("http://img/o.jpg", show.imageorig);
        Assert.False(show.needsupdate); // episode fetch succeeded, so needsupdate clears
        Assert.NotNull(show.Networks);
        Assert.Equal("HBO", show.Networks!.name);
        Assert.Equal("US", show.Networks.country!.code);
    }

    [Fact]
    public async Task RefreshShows_UpdatesExistingShowNeedingUpdate_FromTvMazeResponse()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new
        {
            id = 555,
            name = "Updated Name",
            status = "Ended",
            premiered = "2020-01-01",
            summary = "Updated summary",
            updated = 1700000000,
            weight = 0,
            network = new
            {
                id = 9,
                name = "HBO",
                country = new { name = "United States", code = "US", timezone = "America/New_York" }
            },
            genres = Array.Empty<string>(),
            type = "Scripted",
            language = "English",
            schedule = new { time = "21:00", days = new[] { "Sunday" } },
        });
        httpTest.RespondWithJson(Array.Empty<object>()); // episodes fetch

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Old Name", showid: 555);
            show.needsupdate = true;
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            var result = await service.RefreshShows();
            Assert.True(result);
        }

        using var verify = db.CreateContext();
        var updated = verify.Shows.Find(showId)!;
        Assert.Equal("Updated Name", updated.name);
        Assert.Equal("Ended", updated.status);
        Assert.False(updated.needsupdate);
    }

    [Fact]
    public async Task RefreshShowBatch_ProcessesAllPagesUpToMaxShowPage()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(Array.Empty<object>()); // page 0: no shows returned

        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show", showid: 1);
            show.page = 0;
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        using var ctx2 = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx2);

        Assert.True(await service.RefreshShowBatch());
        httpTest.ShouldHaveCalled("*/shows?page=0");
    }

    [Fact]
    public async Task BacklogPage_RefreshesThePageOfTheFirstShowNeedingUpdate()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(Array.Empty<object>());

        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show", showid: 1);
            show.page = 3;
            show.needsupdate = true;
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        using var ctx2 = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx2);

        Assert.True(await service.BacklogPage());
        httpTest.ShouldHaveCalled("*/shows?page=3");
    }
}
