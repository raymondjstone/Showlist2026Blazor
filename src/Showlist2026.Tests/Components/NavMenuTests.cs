using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Layout;
using Xunit;

namespace Showlist2026.Tests.Components;

public class NavMenuTests : BlazorTestBase
{
    [Fact]
    public void RendersAllTopLevelNavigationLinks()
    {
        var cut = Render<NavMenu>();

        Assert.Contains("href=\"airing\"", cut.Markup);
        Assert.Contains("href=\"calendar\"", cut.Markup);
        Assert.Contains("href=\"admin/tvdirectories\"", cut.Markup);
        Assert.Contains("href=\"filters/genres\"", cut.Markup);
    }
}
