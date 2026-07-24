using Flurl.Http.Testing;
using Microsoft.EntityFrameworkCore;
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
    public async Task RefreshShowDates_SwallowsAndLogs_WhenTvMazeResponseIsUnparseable()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("not valid json", 200);

        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.RefreshShowDates());
        Assert.Empty(ctx.Shows);
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
    public async Task RefreshShowPage_SingleArgOverload_DelegatesToSamePageForFromAndTo()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(Array.Empty<object>());

        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.RefreshShowPage(2));
        httpTest.ShouldHaveCalled("*/shows?page=2");
    }

    [Fact]
    public async Task RefreshShowPage_TreatsPageAsEmpty_WhenTvMazeRequestFails()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("server error", 500);

        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.RefreshShowPage(0, 0));
        Assert.Empty(ctx.Shows);
    }

    [Fact]
    public async Task RefreshShowPage_UsesPlaceholders_WhenNetworkAndWebChannelAreMissing()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new
            {
                id = 43,
                name = "Web-Only New Show",
                status = "Running",
                updated = 1700000000,
                weight = 0,
                genres = Array.Empty<string>(),
                type = "Scripted",
                language = "English",
            }
        });
        httpTest.RespondWithJson(Array.Empty<object>()); // episodes fetch

        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.RefreshShowPage(0, 0));

        var show = ctx.Shows.Single(s => s.showid == 43);
        Assert.Equal("Web-Only New Show", show.name);
    }

    [Fact]
    public async Task RefreshShowPage_LooksUpTimezoneByCountryCode_WhenTvMazeOmitsIt()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new
            {
                id = 44,
                name = "Matched Timezone Show",
                status = "Running",
                updated = 1700000000,
                weight = 0,
                network = new { id = 20, name = "Matched Net", country = new { name = "United States", code = "US" } },
                webChannel = new { id = 21, name = "Unmatched Web", country = new { name = "Nowhere", code = "ZZ" } },
                genres = Array.Empty<string>(),
                type = "Scripted",
                language = "English",
            }
        });
        httpTest.RespondWithJson(Array.Empty<object>()); // episodes fetch

        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            // Matches the network's country code directly (skips the "Unknown" fallback lookup).
            ctx.Timezones.Add(new Showlist2026.Entities.Timezone { countrycode = "US", timezone = "America/New_York" });
            // No entry for "ZZ" - forces the webChannel side through the "Unknown" fallback lookup.
            ctx.Timezones.Add(new Showlist2026.Entities.Timezone { countrycode = "??", timezone = "Unknown" });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.RefreshShowPage(0, 0));
        }

        using var verify = db.CreateContext();
        var show = verify.Shows
            .Include(s => s.Networks)
            .Include(s => s.WebNetworks)
            .Single(s => s.showid == 44);
        Assert.Equal("America/New_York", show.Networks!.timezone);
        Assert.Equal("Unknown", show.WebNetworks!.timezone);
    }

    [Fact]
    public async Task RefreshShowPage_FallsBackToUnknownTimezone_WhenNetworkCountryCodeIsUnmatched()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new
            {
                id = 45,
                name = "Unmatched Network Timezone Show",
                status = "Running",
                updated = 1700000000,
                weight = 0,
                network = new { id = 22, name = "Unmatched Net", country = new { name = "Nowhere", code = "ZZ" } },
                genres = Array.Empty<string>(),
                type = "Scripted",
                language = "English",
            }
        });
        httpTest.RespondWithJson(Array.Empty<object>()); // episodes fetch

        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            // No entry for "ZZ" - forces the network side through the "Unknown" fallback lookup.
            ctx.Timezones.Add(new Showlist2026.Entities.Timezone { countrycode = "??", timezone = "Unknown" });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.RefreshShowPage(0, 0));
        }

        using var verify = db.CreateContext();
        var show = verify.Shows.Include(s => s.Networks).Single(s => s.showid == 45);
        Assert.Equal("Unknown", show.Networks!.timezone);
    }

    [Fact]
    public async Task RefreshShowPage_RemovesOldGenres_AndMatchesExistingGenreEntity_ForShowAlreadyNeedingUpdate()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new
            {
                id = 555,
                name = "Updated Via Page",
                status = "Running",
                updated = 1700000000,
                weight = 0,
                network = new { id = 9, name = "HBO", country = new { name = "United States", code = "US", timezone = "America/New_York" } },
                genres = new[] { "Drama" },
                type = "Scripted",
                language = "English",
            }
        });
        httpTest.RespondWithJson(Array.Empty<object>()); // episodes fetch

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Old Name", showid: 555);
            show.page = 0;
            show.needsupdate = true;
            show.Genres = new List<Showlist2026.Entities.Genre>
            {
                new() { genretext = new Showlist2026.Entities.GenreText { genre = "Old Genre" }, show = show }
            };
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.RefreshShowPage(0, 0));
        }

        using var verify = db.CreateContext();
        var updated = verify.Shows
            .Include(s => s.Genres).ThenInclude(g => g.genretext)
            .Single(s => s.Id == showId);
        Assert.Equal("Updated Via Page", updated.name);
        Assert.Single(updated.Genres!);
        Assert.Equal("Drama", updated.Genres!.Single().genretext!.genre);
        Assert.False(updated.needsupdate);
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
    public async Task RefreshShows_UpdatesExistingShow_WithFullDataAndPreExistingGenres()
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
            url = "http://tvmaze/shows/555",
            network = new
            {
                id = 9,
                name = "HBO",
                country = new { name = "United States", code = "US" } // no timezone -> exercises getTimezone lookup
            },
            webChannel = new
            {
                id = 11,
                name = "HBO Max",
                country = new { name = "United States", code = "US" }
            },
            genres = new[] { "Drama" },
            type = "Scripted",
            language = "English",
            schedule = new { time = "21:00", days = new[] { "Sunday" } },
            image = new { medium = "http://img/m.jpg", original = "http://img/o.jpg" },
            externals = new { tvrage = 12345, thetvdb = 67890, imdb = "tt1234567" },
        });
        httpTest.RespondWithJson(Array.Empty<object>()); // episodes fetch

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Old Name", showid: 555);
            show.needsupdate = true;
            show.Genres = new List<Showlist2026.Entities.Genre>
            {
                new() { genretext = new Showlist2026.Entities.GenreText { genre = "Old Genre" }, show = show }
            };
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.RefreshShows());
        }

        using var verify = db.CreateContext();
        var updated = verify.Shows
            .Include(s => s.Genres).ThenInclude(g => g.genretext)
            .Include(s => s.WebNetworks)
            .Single(s => s.Id == showId);
        Assert.Equal("Updated Name", updated.name);
        Assert.Equal("HBO Max", updated.WebNetworks!.name);
        Assert.Single(updated.Genres!);
        Assert.Equal("Drama", updated.Genres!.Single().genretext!.genre);
        Assert.Equal("12345", updated.tvrage);
        Assert.Equal("http://img/m.jpg", updated.imagemed);
        Assert.False(updated.needsupdate);
    }

    [Fact]
    public async Task RefreshShows_UsesPlaceholders_WhenNetworkAndWebChannelAreMissing()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new
        {
            id = 555,
            name = "Web-Only Show",
            status = "Running",
            updated = 1700000000,
            weight = 0,
            genres = Array.Empty<string>(),
            type = "Scripted",
            language = "English",
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
            Assert.True(await service.RefreshShows());
        }

        using var verify = db.CreateContext();
        var updated = verify.Shows.Find(showId)!;
        Assert.Equal("Web-Only Show", updated.name);
        Assert.False(updated.needsupdate);
    }

    [Fact]
    public async Task RefreshShows_LeavesShowNeedingUpdate_WhenTvMazeRequestFails()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith(status: 500);

        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show", showid: 555);
            show.needsupdate = true;
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.RefreshShows());
        }

        using var verify = db.CreateContext();
        Assert.True(verify.Shows.Find(showId)!.needsupdate);
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

    [Fact]
    public async Task RefreshShowEpisodes_RetriesWithoutSpecials_AndSucceeds_OnRateLimitError()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("rate limited", 429); // first attempt hits "code 429" -> retry
        httpTest.RespondWithJson(new[]
        {
            new { id = 2002, name = "Retry Ep", season = 1, number = 1, airdate = "2024-01-02" }
        });

        using var db = new TestDb();
        var show = TestData.NewShow("My Show", showid: 556);
        using var ctx = db.CreateContext();
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        var result = await service.RefreshShowEpisodes(show);

        Assert.True(result);
        var ep = Assert.Single(show.Episodes!);
        Assert.Equal(2002, ep.episodeid);
        httpTest.ShouldHaveCalled("*/shows/556/episodes"); // retry drops ?specials=1
    }

    [Fact]
    public async Task RefreshShowEpisodes_ReturnsFalse_WhenRetryAfterRateLimitAlsoFails()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("rate limited", 429);
        httpTest.RespondWith("still failing", 500);

        using var db = new TestDb();
        var show = TestData.NewShow("My Show", showid: 557);
        using var ctx = db.CreateContext();
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        var result = await service.RefreshShowEpisodes(show);

        Assert.False(result);
    }

    [Fact]
    public async Task RefreshShowEpisodes_LogsAndSkipsAirtimeAdjustment_WhenItOverflowsMaxDateTime()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            // Airtime is split as "HH:MM" with no bounds checking, so "48:00" contributes 2880
            // minutes (2 days) - added to the last representable date, AddMinutes overflows
            // DateTimeOffset's range and throws inside the inner airtime-adjustment try/catch.
            new { id = 3003, name = "Overflow Ep", season = 1, number = 1, airdate = "9999-12-31", airtime = "48:00" }
        });

        using var db = new TestDb();
        var show = TestData.NewShow("My Show", showid: 558);
        using var ctx = db.CreateContext();
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        var result = await service.RefreshShowEpisodes(show);

        Assert.True(result);
        var ep = Assert.Single(show.Episodes!);
        Assert.Equal(3003, ep.episodeid);
        // The un-adjusted airdate parse still succeeded before the overflow.
        Assert.NotNull(ep.AirDateOffset2);
    }

    [Fact]
    public async Task RefreshShowEpisodes_LogsAndSkipsAirdate_WhenAirdateIsUnparseable()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new { id = 4004, name = "Bad Date Ep", season = 1, number = 1, airdate = "not-a-real-date" }
        });

        using var db = new TestDb();
        var show = TestData.NewShow("My Show", showid: 559);
        using var ctx = db.CreateContext();
        ctx.Shows.Add(show);
        ctx.SaveChanges();

        var service = TestFactory.CreateBackgroundService(ctx);
        var result = await service.RefreshShowEpisodes(show);

        Assert.True(result);
        var ep = Assert.Single(show.Episodes!);
        Assert.Equal(4004, ep.episodeid);
        Assert.Null(ep.AirDateOffset2);
    }

    [Fact]
    public async Task RefreshNetworks_UpdatesExistingNetwork_AndAssignsCountry_WhenPreviouslyUnset()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith(status: 404);
        httpTest.ForCallsTo("*/networks/42").RespondWithJson(new
        {
            id = 42,
            name = "AMC Renamed",
            country = new { name = "United States", code = "US", timezone = "America/New_York" }
        });

        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Networks.Add(new Showlist2026.Entities.Network { networkid = 42, name = "AMC", timezone = "Old/Zone" });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.RefreshNetworks());
        }

        using var verify = db.CreateContext();
        var network = verify.Networks.Include(n => n.country).Single(n => n.networkid == 42);
        Assert.Equal("AMC Renamed", network.name);
        Assert.Equal("America/New_York", network.timezone);
        Assert.Equal("US", network.country!.code);
    }

    [Fact]
    public async Task RefreshNetworks_UsesUnknownCountryPlaceholder_WhenTvMazeOmitsCountry()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith(status: 404);
        httpTest.ForCallsTo("*/networks/7").RespondWithJson(new { id = 7, name = "No Country Network", country = (object?)null });

        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.RefreshNetworks());

        using var verify = db.CreateContext();
        var network = verify.Networks.Include(n => n.country).Single(n => n.networkid == 7);
        Assert.Equal("Unknown", network.country!.name);
        Assert.Equal("??", network.country.code);
    }

    [Fact]
    public async Task RefreshNetworks_ExtendsScanRange_WhenAnExistingNetworkIdExceedsTheDefaultMax()
    {
        // Default scan is IDs 1..1800. Seeding a network with an id above that raises the max
        // (to that id + 50), so a TvMaze response only reachable past 1800 should get picked up.
        using var httpTest = new HttpTest();
        httpTest.RespondWith(status: 404);
        httpTest.ForCallsTo("*/networks/1810").RespondWithJson(new
        {
            id = 1810,
            name = "Beyond Default Range",
            country = new { name = "United States", code = "US", timezone = "America/New_York" }
        });

        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Networks.Add(new Showlist2026.Entities.Network { networkid = 1805, name = "Existing High Id" });
            ctx.SaveChanges();
        }

        using (var ctx = db.CreateContext())
        {
            var service = TestFactory.CreateBackgroundService(ctx);
            Assert.True(await service.RefreshNetworks());
        }

        using var verify = db.CreateContext();
        Assert.Contains(verify.Networks, n => n.networkid == 1810 && n.name == "Beyond Default Range");
    }

    [Fact]
    public async Task RefreshNetworks_CreatesNetworkAndCountry_FromTvMazeResponse()
    {
        // RefreshNetworks always scans network IDs 1..1800 (there's no per-DB lower bound). Only
        // ID 1 resolves to a network here (everything else 404s and is swallowed) - nlist is
        // loaded once before the loop and never refreshed, so if more than one ID resolved to the
        // same network here, each would be re-added as a duplicate (a real, separate bug from
        // what this test is targeting).
        using var httpTest = new HttpTest();
        httpTest.RespondWith(status: 404);
        httpTest.ForCallsTo("*/networks/1").RespondWithJson(new
        {
            id = 42,
            name = "AMC",
            country = new { name = "United States", code = "US", timezone = "America/New_York" }
        });

        using var db = new TestDb();
        using var ctx = db.CreateContext();
        var service = TestFactory.CreateBackgroundService(ctx);

        Assert.True(await service.RefreshNetworks());

        using var verify = db.CreateContext();
        var network = Assert.Single(verify.Networks.Include(n => n.country), n => n.networkid == 42);
        Assert.Equal("AMC", network.name);
        Assert.Equal("US", network.country!.code);
    }
}
