using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListAppServiceStatsSearchTests
{
    [Fact]
    public void GetStatistics_ComputesTotalEpisodesPerShow_ForMostWatchedShows()
    {
        // Regression test: GetStatistics used to run a separate COUNT query per watched show
        // (N+1) to populate ShowWatchStat.TotalEpisodes. Verifies the value is still correct
        // after replacing it with a single grouped query.
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show", wanted: true, status: "Running");
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10), watched: true);
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-9), watched: true);
            TestData.NewEpisode(show, 1, 3, DateTimeOffset.UtcNow.AddDays(-8)); // unwatched, still counts toward total
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var stats = service.GetStatistics();

        Assert.Equal(1, stats.TotalShowsTracked);
        Assert.Equal(1, stats.ActiveShows);
        Assert.Equal(0, stats.CompletedShows);
        Assert.Equal(2, stats.TotalEpisodesWatched);

        var stat = Assert.Single(stats.MostWatchedShows);
        Assert.Equal("Show", stat.ShowName);
        Assert.Equal(2, stat.EpisodesWatched);
        Assert.Equal(3, stat.TotalEpisodes); // all 3 episodes, not just watched ones
    }

    [Fact]
    public void GetStatistics_CountsCompletedShows()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var ended = TestData.NewShow("Ended Show", wanted: true, status: "Ended");
            var running = TestData.NewShow("Running Show", wanted: true, status: "Running");
            ctx.Shows.AddRange(ended, running);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var stats = service.GetStatistics();

        Assert.Equal(1, stats.CompletedShows);
        Assert.Equal(1, stats.ActiveShows);
    }

    [Fact]
    public void AdvancedSearch_FiltersByNameAndPagesResults()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            for (int i = 1; i <= 3; i++)
                ctx.Shows.Add(TestData.NewShow($"Breaking Bad Spinoff {i}"));
            ctx.Shows.Add(TestData.NewShow("Unrelated Show"));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);

        var (allMatches, totalCount) = service.AdvancedSearch(name: "Breaking", genreId: null, networkId: null, year: null, page: 1, pageSize: 50);
        Assert.Equal(3, totalCount);
        Assert.Equal(3, allMatches.Count);

        var (page1, _) = service.AdvancedSearch(name: "Breaking", genreId: null, networkId: null, year: null, page: 1, pageSize: 2);
        Assert.Equal(2, page1.Count);

        var (page2, _) = service.AdvancedSearch(name: "Breaking", genreId: null, networkId: null, year: null, page: 2, pageSize: 2);
        Assert.Single(page2);
    }

    [Fact]
    public void AdvancedSearch_FiltersByWantedState()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Wanted", wanted: true));
            ctx.Shows.Add(TestData.NewShow("Excluded", wanted: false));
            ctx.Shows.Add(TestData.NewShow("Undecided"));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);

        var (wanted, _) = service.AdvancedSearch(null, null, null, null, wanted: "wanted");
        Assert.Single(wanted);
        Assert.Equal("Wanted", wanted[0].name);

        var (undecided, _) = service.AdvancedSearch(null, null, null, null, wanted: "undecided");
        Assert.Single(undecided);
        Assert.Equal("Undecided", undecided[0].name);
    }

    [Fact]
    public void GetSimilarShows_RanksByGenreOverlap_AndExcludesDecidedShows()
    {
        using var db = new TestDb();
        int targetId;
        using (var ctx = db.CreateContext())
        {
            var scifi = TestData.NewGenreText("Sci-Fi");
            var drama = TestData.NewGenreText("Drama");
            ctx.GenreTexts.AddRange(scifi, drama);

            var target = TestData.NewShow("Target Show");
            var target_genres = new List<Showlist2026.Entities.Genre>
            {
                new() { show = target, genretext = scifi },
                new() { show = target, genretext = drama }
            };

            var bothGenres = TestData.NewShow("Both Genres Match"); // undecided -> eligible
            var oneGenre = TestData.NewShow("One Genre Match"); // undecided -> eligible
            var alreadyWanted = TestData.NewShow("Already Wanted", wanted: true); // decided -> excluded

            ctx.Shows.AddRange(target, bothGenres, oneGenre, alreadyWanted);
            ctx.Genres.AddRange(target_genres);
            ctx.Genres.Add(new Showlist2026.Entities.Genre { show = bothGenres, genretext = scifi });
            ctx.Genres.Add(new Showlist2026.Entities.Genre { show = bothGenres, genretext = drama });
            ctx.Genres.Add(new Showlist2026.Entities.Genre { show = oneGenre, genretext = scifi });
            ctx.Genres.Add(new Showlist2026.Entities.Genre { show = alreadyWanted, genretext = scifi });
            ctx.Genres.Add(new Showlist2026.Entities.Genre { show = alreadyWanted, genretext = drama });
            ctx.SaveChanges();
            targetId = target.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.GetSimilarShows(targetId, max: 5);

        Assert.Equal(2, results.Count);
        Assert.Equal("Both Genres Match", results[0].name); // ranked first: 2 matching genres
        Assert.Equal("One Genre Match", results[1].name);
        Assert.DoesNotContain(results, s => s.name == "Already Wanted");
    }

    [Fact]
    public void GetSimilarShows_ReturnsEmpty_WhenTargetShowHasNoGenres()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("No Genres");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        Assert.Empty(service.GetSimilarShows(showId));
    }

    [Fact]
    public void FindDuplicateShows_ReturnsShowsSharingTheSameTvMazeId()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Dupe A", showid: 100));
            ctx.Shows.Add(TestData.NewShow("Dupe B", showid: 100));
            ctx.Shows.Add(TestData.NewShow("Unique", showid: 200));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.FindDuplicateShows();

        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.Equal(100, s.showid));
    }

    [Fact]
    public void CompareShows_ComputesEpisodeCountsAndGenres()
    {
        using var db = new TestDb();
        int id1, id2;
        using (var ctx = db.CreateContext())
        {
            var genre = TestData.NewGenreText("Drama");
            ctx.GenreTexts.Add(genre);

            var show1 = TestData.NewShow("Show One");
            TestData.NewEpisode(show1, 1, 1, DateTimeOffset.UtcNow.AddDays(-5), watched: true);
            TestData.NewEpisode(show1, 1, 2, DateTimeOffset.UtcNow.AddDays(5));

            var show2 = TestData.NewShow("Show Two");

            ctx.Shows.AddRange(show1, show2);
            ctx.Genres.Add(new Showlist2026.Entities.Genre { show = show1, genretext = genre });
            ctx.SaveChanges();
            id1 = show1.Id;
            id2 = show2.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var comparison = service.CompareShows(id1, id2);

        Assert.Equal(2, comparison.Show1.TotalEpisodes);
        Assert.Equal(1, comparison.Show1.AiredEpisodes);
        Assert.Equal(1, comparison.Show1.WatchedEpisodes);
        Assert.Equal("Drama", comparison.Show1.Genres);
        Assert.Equal(0, comparison.Show2.TotalEpisodes);
    }

    [Fact]
    public void GetDownloadProgress_ComputesMissingEpisodesForWantedShows()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show", wanted: true);
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10), watched: true); // downloaded (watched)
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-5)); // aired, not downloaded
            TestData.NewEpisode(show, 1, 3, DateTimeOffset.UtcNow.AddDays(5)); // not aired
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.GetDownloadProgress();

        var progress = Assert.Single(results);
        Assert.Equal(2, progress.TotalAiredEpisodes);
        Assert.Equal(1, progress.DownloadedEpisodes);
        Assert.Equal(1, progress.MissingCount);
        Assert.Equal(50, progress.PercentComplete);
        Assert.Single(progress.MissingEpisodes);
        Assert.Equal(2, progress.MissingEpisodes[0].number);
    }

    [Fact]
    public void GetEpisodeCountsForShows_ReturnsWatchedAndTotalPerShow()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            TestData.NewEpisode(show, 1, 1, watched: true);
            TestData.NewEpisode(show, 1, 2);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var counts = service.GetEpisodeCountsForShows(new List<int> { showId, 999 });

        Assert.Equal((1, 2), counts[showId]);
        Assert.Equal((0, 0), counts[999]); // unknown id defaults to (0,0)
    }
}
