using Flurl.Http.Testing;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListAppServiceHttpTests
{
    [Fact]
    public async Task SearchTvMaze_ReturnsResults_FromTvMazeSearchEndpoint()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new
            {
                score = 5.0,
                show = new
                {
                    id = 1,
                    name = "Breaking Bad",
                    type = "Scripted",
                    language = "English",
                    status = "Ended",
                    premiered = "2008-01-20",
                    summary = "A chemistry teacher...",
                    image = new { medium = "http://img/m.jpg", original = "http://img/o.jpg" }
                }
            }
        });

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var results = await service.SearchTvMaze("Breaking Bad");

        var result = Assert.Single(results);
        Assert.Equal("Breaking Bad", result.Show.Name);
        Assert.Equal("Ended", result.Show.Status);
        Assert.Equal("http://img/m.jpg", result.Show.Image!.Medium);
        httpTest.ShouldHaveCalled("*/search/shows?q=Breaking%20Bad");
    }

    [Fact]
    public async Task SearchTvMaze_ReturnsEmptyList_OnHttpFailure()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("error", 500);

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var results = await service.SearchTvMaze("Anything");

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetTrendingShows_ExcludesIgnoredShowsAndExcludedTypes_AndCountsRepeatedEpisodes()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new object[]
        {
            new { show = new { id = 1, name = "Show A", type = "Scripted", network = (object?)null, image = (object?)null, status = "Running" } },
            new { show = new { id = 1, name = "Show A", type = "Scripted", network = (object?)null, image = (object?)null, status = "Running" } }, // second episode airing same day -> increments count
            new { show = new { id = 2, name = "Ignored Show", type = "Scripted", network = (object?)null, image = (object?)null, status = "Running" } },
            new { show = new { id = 3, name = "Excluded Type Show", type = "Talk Show", network = (object?)null, image = (object?)null, status = "Running" } },
        });

        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var ignored = TestData.NewShow("Ignored Show", showid: 2, wanted: false);
            var excludedType = TestData.NewType("Talk Show", wanted: false);
            ctx.Shows.Add(ignored);
            ctx.Types.Add(excludedType);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = await service.GetTrendingShows();

        Assert.Single(results); // only "Show A" survives both exclusion filters
        Assert.Equal("Show A", results[0].Name);
        Assert.Equal(2, results[0].EpisodeCount); // counted twice
    }

    [Fact]
    public async Task GetTrendingShows_MarksLocallyTrackedShowAsAlreadyTracked()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new object[]
        {
            new { show = new { id = 10, name = "Known Show", type = "Scripted", network = (object?)null, image = (object?)null, status = "Running" } },
        });

        using var db = new TestDb();
        int localId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Known Show", showid: 10);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            localId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var results = await service.GetTrendingShows();

        var result = Assert.Single(results);
        Assert.True(result.AlreadyTracked);
        Assert.Equal(localId, result.LocalShowId);
    }

    [Fact]
    public async Task GetTrendingShows_ReturnsEmptyList_OnHttpFailure()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("error", 500);

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        Assert.Empty(await service.GetTrendingShows());
    }
}
