using Bunit;
using Showlist2026.Entities;
using Showlist2026.Models;
using Showlist2026.Web.Components.Shared;
using Type = Showlist2026.Entities.Type;
using Xunit;

namespace Showlist2026.Tests.Components;

public class NewShowCardTests : Bunit.BunitContext
{
    private static EpFilter MakeEpFilter(Show show, Episode? ep = null)
    {
        ep ??= new Episode { show = show, season = 1, number = 1, AirDateOffset2 = DateTimeOffset.UtcNow };
        show.Episodes ??= new List<Episode> { ep };
        return new EpFilter(ep, new List<TVSite>());
    }

    [Fact]
    public void RendersNothing_WhenShowFilterHasNoShow()
    {
        var cut = Render<NewShowCard>(p => p.Add(c => c.ShowFilter, new EpFilter(new List<TVSite>())));

        Assert.Equal("", cut.Markup.Trim());
    }

    [Fact]
    public void RendersTypeGenresLanguageNetworkAndWebNetwork_WithCountryCodes()
    {
        var show = new Show
        {
            Id = 1,
            name = "Breaking Bad",
            Types = new Type { Id = 1, type = "Scripted" },
            Languages = new Language { Id = 1, name = "English" },
            Networks = new Network { Id = 1, name = "AMC", country = new Country { Id = 1, code = "US" } },
            WebNetworks = new WebNetwork { Id = 1, name = "Netflix", country = new Country { Id = 2, code = "GB" } },
            Genres = new List<Genre>
            {
                new() { Id = 1, genretext = new GenreText { Id = 1, genre = "Drama" } },
                new() { Id = 2, genretext = new GenreText { Id = 2, genre = "Crime" } }
            },
            summary = "A chemistry teacher",
        };
        var ef = MakeEpFilter(show);

        var cut = Render<NewShowCard>(p => p.Add(c => c.ShowFilter, ef));

        Assert.Contains("Breaking Bad", cut.Markup);
        Assert.Contains("Scripted", cut.Markup);
        Assert.Contains("Drama", cut.Markup);
        Assert.Contains("Crime", cut.Markup);
        Assert.Contains("English", cut.Markup);
        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("(", cut.Markup);
        Assert.Contains("US", cut.Markup);
        Assert.Contains("Netflix", cut.Markup);
        Assert.Contains("GB", cut.Markup);
        Assert.Contains("A chemistry teacher", cut.Markup);
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

        var cut = Render<NewShowCard>(p => p.Add(c => c.ShowFilter, ef));

        Assert.Contains("AMC", cut.Markup);
        Assert.DoesNotContain("??", cut.Markup);
    }

    [Fact]
    public void RendersOnlyOneEpisodeRow_WhenShowHasASingleEpisode()
    {
        var show = new Show { Id = 1, name = "Show" };
        var ep = new Episode { show = show, season = 1, number = 1, AirDateOffset2 = DateTimeOffset.UtcNow, name = "Pilot" };
        var ef = MakeEpFilter(show, ep);

        var cut = Render<NewShowCard>(p => p.Add(c => c.ShowFilter, ef));

        Assert.Single(cut.FindAll("div.row.bg-light"));
        Assert.Contains("Pilot", cut.Markup);
    }

    [Fact]
    public void RendersFirstAndLastEpisodeRows_WhenShowHasMultipleEpisodes()
    {
        var show = new Show { Id = 1, name = "Show" };
        var first = new Episode { Id = 1, show = show, season = 1, number = 1, AirDateOffset2 = DateTimeOffset.UtcNow.AddDays(-10), name = "Pilot" };
        var last = new Episode { Id = 2, show = show, season = 1, number = 5, AirDateOffset2 = DateTimeOffset.UtcNow.AddDays(20), name = "Finale" };
        show.Episodes = new List<Episode> { first, last };
        var ef = new EpFilter(first, new List<TVSite>());

        var cut = Render<NewShowCard>(p => p.Add(c => c.ShowFilter, ef));

        Assert.Equal(2, cut.FindAll("div.row.bg-light").Count);
        Assert.Contains("Pilot", cut.Markup);
        Assert.Contains("Finale", cut.Markup);
    }

    [Fact]
    public void ClickingSelectFilterButton_RaisesOnFilterChangedForShow()
    {
        var show = new Show { Id = 42, name = "Show" };
        var ef = MakeEpFilter(show);
        (long id, string type, bool? state)? received = null;

        var cut = Render<NewShowCard>(p => p
            .Add(c => c.ShowFilter, ef)
            .Add(c => c.OnFilterChanged, args => received = args));

        cut.Find("i.far.fa-check-circle").Click();

        Assert.Equal((42L, "show", (bool?)true), received);
    }
}
