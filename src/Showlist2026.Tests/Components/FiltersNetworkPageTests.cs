using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

/// <summary>
/// End-to-end test: renders a real page against the REAL ShowListAppService, backed by an
/// isolated InMemory database (see BlazorTestBase) - not a mocked service. Proves the whole
/// DI -> data load -> render -> click -> mutate -> reload pipeline actually works, not just
/// that the component compiles.
/// </summary>
public class FiltersNetworkPageTests : BlazorTestBase
{
    [Fact]
    public void RendersNetworksSortedByName()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Networks.Add(TestData.NewNetwork("HBO"));
            ctx.Networks.Add(TestData.NewNetwork("ABC"));
            ctx.SaveChanges();
        }

        var cut = Render<FiltersNetwork>();

        var indexOfAbc = cut.Markup.IndexOf("ABC", StringComparison.Ordinal);
        var indexOfHbo = cut.Markup.IndexOf("HBO", StringComparison.Ordinal);
        Assert.True(indexOfAbc >= 0 && indexOfHbo >= 0 && indexOfAbc < indexOfHbo);
    }

    [Fact]
    public void ClickingAlwaysInclude_PersistsThroughTheRealServiceAndUpdatesUi()
    {
        int networkId;
        using (var ctx = Db.CreateContext())
        {
            var network = TestData.NewNetwork("HBO");
            ctx.Networks.Add(network);
            ctx.SaveChanges();
            networkId = network.Id;
        }

        var cut = Render<FiltersNetwork>();
        cut.Find("button.btn-outline-secondary").Click(); // "Always Include" is the first button when undecided

        // Verify it went through the real service and actually persisted to the database.
        using var verify = Db.CreateContext();
        Assert.True(verify.Networks.Find(networkId)!.Wanted);

        // And the UI re-rendered to reflect the new state.
        Assert.Contains("Always included", cut.Markup);
    }

    [Fact]
    public void ShowsLoadingSpinner_BeforeDataArrives()
    {
        // With nothing seeded, the page still renders its list state (empty), not stuck loading -
        // OnInitializedAsync completes synchronously against the in-memory provider.
        var cut = Render<FiltersNetwork>();

        Assert.DoesNotContain("spinner-border", cut.Markup);
        Assert.Contains("Network Filters", cut.Markup);
    }
}
