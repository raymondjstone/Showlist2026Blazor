using Bunit;
using Showlist2026.Entities;
using Showlist2026.Models;
using Showlist2026.Web.Components.Shared;
using Xunit;

namespace Showlist2026.Tests.Components;

// NewShowTab isn't referenced anywhere in the app (dead/orphaned component - superseded by the
// tab-switching logic that pages like Undecided.razor implement inline), so it's rendered
// directly here rather than through a page.
public class NewShowTabTests : Bunit.BunitContext
{
    [Fact]
    public void RendersNothing_WhenTabIsNull()
    {
        var cut = Render<NewShowTab>(p => p.Add(c => c.Tab, null));

        Assert.Equal("", cut.Markup.Trim());
    }

    [Fact]
    public void RendersANewShowCardPerEpisodeInTheTab()
    {
        var show = new Show { Id = 1, name = "Show" };
        var ep = new Episode { show = show, season = 1, number = 1, AirDateOffset2 = DateTimeOffset.UtcNow };
        var ef = new EpFilter(ep, new List<TVSite>());
        var tab = new AiringAroundNowTabModel("New", new List<EpFilter> { ef });

        var cut = Render<NewShowTab>(p => p.Add(c => c.Tab, tab));

        Assert.Contains("Show", cut.Markup);
        Assert.Contains("TNew", cut.Markup);
    }
}
