using Showlist2026.TVMaze.TVMazeEpisodes;
using Showlist2026.TVMaze.TVMazePage;
using Xunit;

namespace Showlist2026.Tests.Models;

/// <summary>
/// Plain get/set round-trips for TVMaze API response DTOs. These properties have no logic of
/// their own - they exist purely to be populated by JSON deserialization - so there's nothing to
/// assert beyond "the value that was set comes back out."
/// </summary>
public class TVMazeDtoPropertyTests
{
    [Fact]
    public void TVMazeShowData_ExposesRuntimeAndOfficialSite()
    {
        var show = new TVMazeShowData { Runtime = 60, OfficialSite = "http://example.com" };

        Assert.Equal(60, show.Runtime);
        Assert.Equal("http://example.com", show.OfficialSite);
    }

    [Fact]
    public void Links_ExposesPreviousAndNextEpisode()
    {
        var prev = new Nextepisode { Href = "http://tvmaze/episodes/1" };
        var next = new Nextepisode { Href = "http://tvmaze/episodes/2" };
        var links = new Showlist2026.TVMaze.TVMazePage.Links { Previousepisode = prev, Nextepisode = next };

        Assert.Same(prev, links.Previousepisode);
        Assert.Same(next, links.Nextepisode);
    }

    [Fact]
    public void EpisodeData_RoundTripsUrlTypeRuntimeAndLinks()
    {
        var links = new Showlist2026.TVMaze.TVMazeEpisodes.Links { Self = new Self { Href = "http://tvmaze/episodes/1" } };
        var ep = new EpisodeData { Url = "http://tvmaze/episodes/1", Type = "regular", Runtime = 45, Links = links };

        Assert.Equal("http://tvmaze/episodes/1", ep.Url);
        Assert.Equal("regular", ep.Type);
        Assert.Equal(45, ep.Runtime);
        Assert.Same(links, ep.Links);
    }
}
