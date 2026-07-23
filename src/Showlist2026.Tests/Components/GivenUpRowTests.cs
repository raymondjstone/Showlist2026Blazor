using Bunit;
using Showlist2026.Entities;
using Showlist2026.Models;
using Showlist2026.Web.Components.Shared;
using Type = Showlist2026.Entities.Type;
using Xunit;

namespace Showlist2026.Tests.Components;

public class GivenUpRowTests : Bunit.BunitContext
{
    private static EpFilter MakeEpFilter(Show show, Episode? ep = null)
    {
        ep ??= new Episode { show = show, season = 1, number = 1, AirDateOffset2 = DateTimeOffset.UtcNow };
        return new EpFilter(ep, new List<TVSite>());
    }

    [Fact]
    public void RendersNothing_WhenEpisodeIsNull()
    {
        var cut = Render<GivenUpRow>(p => p.Add(c => c.Episode, new EpFilter(new List<TVSite>())));

        Assert.Equal("", cut.Markup.Trim());
    }

    [Fact]
    public void RendersTypeGenresLanguageNetworkWebNetworkAndStatusBadge()
    {
        var show = new Show
        {
            Id = 1,
            name = "Breaking Bad",
            status = "Running",
            Types = new Type { Id = 1, type = "Scripted" },
            Languages = new Language { Id = 1, name = "English" },
            Networks = new Network { Id = 1, name = "AMC", country = new Country { Id = 1, code = "US" } },
            WebNetworks = new WebNetwork { Id = 1, name = "Netflix", country = new Country { Id = 2, code = "GB" } },
            Genres = new List<Genre>
            {
                new() { Id = 1, genretext = new GenreText { Id = 1, genre = "Drama" } },
                new() { Id = 2, genretext = new GenreText { Id = 2, genre = "Crime" } }
            },
        };
        var ef = MakeEpFilter(show);

        var cut = Render<GivenUpRow>(p => p.Add(c => c.Episode, ef));

        Assert.Contains("Scripted", cut.Markup);
        Assert.Contains("Drama", cut.Markup);
        Assert.Contains("Crime", cut.Markup);
        Assert.Contains("English", cut.Markup);
        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("US", cut.Markup);
        Assert.Contains("Netflix", cut.Markup);
        Assert.Contains("GB", cut.Markup);
        Assert.Contains("bg-success\">Running", cut.Markup);
    }

    [Fact]
    public void OmitsCountryParens_WhenCountryCodeIsUnknown()
    {
        var show = new Show
        {
            Id = 1,
            name = "Some Show",
            Networks = new Network { Id = 1, name = "AMC", country = new Country { Id = 1, code = "??" } },
        };
        var ef = MakeEpFilter(show);

        var cut = Render<GivenUpRow>(p => p.Add(c => c.Episode, ef));

        Assert.Contains("AMC", cut.Markup);
        Assert.DoesNotContain("??", cut.Markup);
    }

    [Fact]
    public void ClickingUndo_RaisesOnUndoGivenUp_WithStateFalse()
    {
        var show = new Show { Id = 1, name = "Show" };
        var ep = new Episode { Id = 99, show = show, season = 1, number = 1 };
        var ef = new EpFilter(ep, new List<TVSite>());
        (long id, bool state)? received = null;

        var cut = Render<GivenUpRow>(p => p
            .Add(c => c.Episode, ef)
            .Add(c => c.OnUndoGivenUp, args => received = args));

        cut.Find("i.fa-undo").Click();

        Assert.Equal((99L, false), received);
    }
}
