using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class UndecidedPageTests : BlazorTestBase
{
    [Fact]
    public void RendersEmptyTabList_WhenNothingIsUndecided()
    {
        // AppService.UndecidedShows()'s "attach latest episode" step uses a raw SQL query that
        // the InMemory test provider can't run (see ShowListAppServiceQueryTests for the same
        // documented gap) - it's only reached when there's at least one eligible show. This
        // test exercises the page's empty-state rendering, which is unaffected by that gap.
        var cut = Render<Undecided>();

        Assert.DoesNotContain("spinner-border", cut.Markup);
        Assert.Empty(cut.FindAll("li.nav-item"));
    }
}
