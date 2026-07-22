using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class AdvancedSearchPageTests : BlazorTestBase
{
    [Fact]
    public void SearchingByName_ShowsMatchingLocalResults()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad"));
            ctx.Shows.Add(TestData.NewShow("Better Call Saul"));
            ctx.SaveChanges();
        }

        var cut = Render<AdvancedSearch>();

        cut.Find("input[placeholder='Optional']").Change("Breaking");
        cut.Find("button.btn-primary").Click();

        Assert.Contains("Breaking Bad", cut.Markup);
        Assert.DoesNotContain("Better Call Saul", cut.Markup);
    }

    [Fact]
    public void ClearFilters_ResetsResults()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad"));
            ctx.SaveChanges();
        }

        var cut = Render<AdvancedSearch>();
        cut.Find("input[placeholder='Optional']").Change("Breaking");
        cut.Find("button.btn-primary").Click();
        Assert.Contains("Local Results", cut.Markup);

        cut.Find("button.btn-outline-secondary[title='Clear all filters']").Click();

        Assert.DoesNotContain("Local Results", cut.Markup);
    }
}
