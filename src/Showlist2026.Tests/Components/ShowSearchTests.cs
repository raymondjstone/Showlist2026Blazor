using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Components;

public class ShowSearchTests : BlazorTestBase
{
    [Fact]
    public async Task TypingTwoOrMoreCharacters_ShowsMatchingShows()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad"));
            ctx.SaveChanges();
        }

        var cut = Render<Showlist2026.Web.Components.Shared.ShowSearch>();
        cut.Find("input").FocusIn(new FocusEventArgs());
        await cut.Find("input").InputAsync("Breaking");

        cut.WaitForAssertion(() => Assert.Contains("Breaking Bad", cut.Markup), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task SelectingAResult_NavigatesToItsShowDetailPage()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Breaking Bad");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<Showlist2026.Web.Components.Shared.ShowSearch>();
        cut.Find("input").FocusIn(new FocusEventArgs());
        await cut.Find("input").InputAsync("Breaking");
        cut.WaitForAssertion(() => Assert.Contains("Breaking Bad", cut.Markup), TimeSpan.FromSeconds(3));

        cut.Find("div[style*='cursor: pointer']").Click();

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.EndsWith($"/showlist/show/{showId}", nav.Uri);
    }
}
