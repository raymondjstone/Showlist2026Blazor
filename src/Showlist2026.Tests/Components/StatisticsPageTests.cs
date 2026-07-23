using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class StatisticsPageTests : BlazorTestBase
{
    [Fact]
    public void RendersShowAndWatchTimeSummary()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Show", wanted: true, status: "Running");
            TestData.NewEpisode(show, 1, 1, watched: true, runtime: "60");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<Statistics>();

        Assert.Contains("Shows Tracked", cut.Markup);
        Assert.Contains("1</p>", cut.Markup); // TotalShowsTracked
        Assert.Contains("1h", cut.Markup); // 60 minutes watched
    }

    [Fact]
    public void OmitsMostWatchedShowsSection_WhenNothingWatched()
    {
        var cut = Render<Statistics>();

        Assert.DoesNotContain("Most Watched Shows", cut.Markup);
        Assert.DoesNotContain("Genre Breakdown", cut.Markup);
    }

    [Fact]
    public void RendersMostWatchedShowsGenreBreakdownAndEpisodesPerMonth()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Show", wanted: true, status: "Running");
            show.Genres = new List<Showlist2026.Entities.Genre>
            {
                new() { genretext = TestData.NewGenreText("Drama"), show = show }
            };
            TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10), watched: true, runtime: "60");
            TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-9)); // unwatched, counts toward TotalEpisodes
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var cut = Render<Statistics>();

        Assert.Contains("Most Watched Shows", cut.Markup);
        Assert.Contains("Show", cut.Markup);
        Assert.Contains("1 / 2", cut.Markup); // 1 watched of 2 total episodes

        Assert.Contains("Genre Breakdown", cut.Markup);
        Assert.Contains("Drama", cut.Markup);
        Assert.Contains("1 shows", cut.Markup);

        Assert.Contains("Episodes Watched Per Month", cut.Markup);
        Assert.Contains("1 episodes", cut.Markup);
    }
}
