using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class ComparePageTests : BlazorTestBase
{
    [Fact]
    public async Task SearchingAndSelectingBothShows_ThenComparing_RendersRealComparisonData()
    {
        int show1Id, show2Id;
        using (var ctx = Db.CreateContext())
        {
            var show1 = TestData.NewShow("Breaking Bad");
            TestData.NewEpisode(show1, 1, 1, DateTimeOffset.UtcNow.AddDays(-10), watched: true);
            var show2 = TestData.NewShow("Better Call Saul");
            ctx.Shows.AddRange(show1, show2);
            ctx.SaveChanges();
            show1Id = show1.Id;
            show2Id = show2.Id;
        }

        var cut = Render<Compare>();
        var inputs = cut.FindAll("input.form-control");

        await inputs[0].InputAsync("Breaking");
        cut.WaitForAssertion(() => Assert.Contains("Breaking Bad", cut.Markup));
        cut.Find("li.list-group-item").Click(); // selects show1

        var inputs2 = cut.FindAll("input.form-control");
        await inputs2[1].InputAsync("Better");
        cut.WaitForAssertion(() => Assert.Contains("Better Call Saul", cut.Markup));
        cut.Find("li.list-group-item").Click(); // selects show2

        cut.Find("button.btn-primary").Click();

        Assert.Contains("Breaking Bad", cut.Markup);
        Assert.Contains("Better Call Saul", cut.Markup);
        Assert.Contains("Watched Episodes", cut.Markup);
    }
}
