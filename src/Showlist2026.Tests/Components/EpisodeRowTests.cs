using Bunit;
using Showlist2026.Entities;
using Showlist2026.Models;
using Showlist2026.Web.Components.Shared;
using Xunit;

namespace Showlist2026.Tests.Components;

public class EpisodeRowTests : Bunit.BunitContext
{
    private static EpFilter MakeEpFilter(Show show, Episode? ep = null)
    {
        ep ??= new Episode { show = show, season = 1, number = 1, Id = 100 };
        return new EpFilter(ep, new List<TVSite>());
    }

    [Fact]
    public void RendersNothing_WhenEpisodeIsNull()
    {
        var cut = Render<EpisodeRow>(p => p.Add(c => c.Episode, new EpFilter(new List<TVSite>())));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void RendersShowNameAndEpisodeNumber()
    {
        var show = new Show { Id = 1, name = "My Show" };
        var ef = MakeEpFilter(show);

        var cut = Render<EpisodeRow>(p => p.Add(c => c.Episode, ef));

        Assert.Contains("My Show", cut.Markup);
        Assert.Contains("S01E01", cut.Markup);
    }

    [Theory]
    [InlineData("Running", "bg-success")]
    [InlineData("Ended", "bg-secondary")]
    [InlineData("To Be Determined", "bg-warning")]
    [InlineData("In Development", "bg-info")]
    [InlineData(null, "bg-light")]
    public void RendersCorrectStatusBadgeClass(string? status, string expectedClass)
    {
        var show = new Show { Id = 1, name = "Show", status = status };
        var ef = MakeEpFilter(show);

        var cut = Render<EpisodeRow>(p => p.Add(c => c.Episode, ef));

        var badge = cut.Find("span.badge");
        Assert.Contains(expectedClass, badge.ClassList);
    }

    [Fact]
    public void ClickingEyeIcon_InvokesOnWatchedChanged_WithEpisodeIdAndTrue()
    {
        var show = new Show { Id = 1, name = "Show" };
        var ep = new Episode { show = show, season = 1, number = 1, Id = 555 };
        var ef = MakeEpFilter(show, ep);
        (long id, bool state)? received = null;

        var cut = Render<EpisodeRow>(p => p
            .Add(c => c.Episode, ef)
            .Add(c => c.OnWatchedChanged, args => received = args));

        cut.Find("i.fa-eye").Click();

        Assert.Equal((555L, true), received);
    }

    [Fact]
    public void GivenUpIcon_OnlyRenders_WhenCallbackHasDelegate()
    {
        var show = new Show { Id = 1, name = "Show" };
        var ef = MakeEpFilter(show);

        var withoutCallback = Render<EpisodeRow>(p => p.Add(c => c.Episode, ef));
        Assert.DoesNotContain("fa-flag", withoutCallback.Markup);

        var withCallback = Render<EpisodeRow>(p => p
            .Add(c => c.Episode, ef)
            .Add(c => c.OnGivenUpChanged, _ => { }));
        Assert.Contains("fa-flag", withCallback.Markup);
    }

    [Fact]
    public void ClickingGivenUpIcon_InvokesOnGivenUpChanged_WithEpisodeIdAndTrue()
    {
        var show = new Show { Id = 1, name = "Show" };
        var ep = new Episode { show = show, season = 1, number = 1, Id = 777 };
        var ef = MakeEpFilter(show, ep);
        (long id, bool state)? received = null;

        var cut = Render<EpisodeRow>(p => p
            .Add(c => c.Episode, ef)
            .Add(c => c.OnGivenUpChanged, args => received = args));

        cut.Find("i.fa-flag").Click();

        Assert.Equal((777L, true), received);
    }

    [Fact]
    public void CatchUpIcon_OnlyRenders_WhenCallbackHasDelegate()
    {
        var show = new Show { Id = 1, name = "Show" };
        var ef = MakeEpFilter(show);

        var withoutCallback = Render<EpisodeRow>(p => p.Add(c => c.Episode, ef));
        Assert.DoesNotContain("fa-check-double", withoutCallback.Markup);

        var withCallback = Render<EpisodeRow>(p => p
            .Add(c => c.Episode, ef)
            .Add(c => c.OnCatchUpShow, _ => { }));
        Assert.Contains("fa-check-double", withCallback.Markup);
    }

    [Fact]
    public void ClickingCatchUpIcon_InvokesOnCatchUpShow_WithShowId()
    {
        var show = new Show { Id = 321, name = "Show" };
        var ef = MakeEpFilter(show);
        long? received = null;

        var cut = Render<EpisodeRow>(p => p
            .Add(c => c.Episode, ef)
            .Add(c => c.OnCatchUpShow, id => received = id));

        cut.Find("a[title='Catch up all missed for this show']").Click();

        Assert.Equal(321L, received);
    }

    [Fact]
    public void RendersNetworkAndCountryCode_WhenPresent()
    {
        var show = new Show
        {
            Id = 1,
            name = "Show",
            Networks = new Network { Id = 5, name = "HBO", country = new Country { Id = 2, code = "US" } }
        };
        var ef = MakeEpFilter(show);

        var cut = Render<EpisodeRow>(p => p.Add(c => c.Episode, ef));

        Assert.Contains("HBO", cut.Markup);
        Assert.Contains("US", cut.Markup);
    }

    [Fact]
    public void FolderCreationLink_OnlyRenders_WhenShowHasNoFolderName()
    {
        var showNoFolder = new Show { Id = 1, name = "Show" };
        var showWithFolder = new Show { Id = 2, name = "Show", FolderName = "Show.Folder" };

        var cutNoFolder = Render<EpisodeRow>(p => p.Add(c => c.Episode, MakeEpFilter(showNoFolder)));
        var cutWithFolder = Render<EpisodeRow>(p => p.Add(c => c.Episode, MakeEpFilter(showWithFolder)));

        Assert.Contains("fa-folder-plus", cutNoFolder.Markup);
        Assert.DoesNotContain("fa-folder-plus", cutWithFolder.Markup);
    }
}
