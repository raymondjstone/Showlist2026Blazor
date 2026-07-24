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

    [Fact]
    public async Task ArrowDownThenEnter_NavigatesToTheHighlightedShow()
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

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.EndsWith($"/showlist/show/{showId}", nav.Uri);
    }

    [Fact]
    public async Task ArrowUpThenEnter_NavigatesToTheHighlightedShow()
    {
        int firstShowId;
        using (var ctx = Db.CreateContext())
        {
            var first = TestData.NewShow("Breaking Bad");
            ctx.Shows.Add(first);
            ctx.Shows.Add(TestData.NewShow("Breaking News"));
            ctx.SaveChanges();
            firstShowId = first.Id;
        }

        var cut = Render<Showlist2026.Web.Components.Shared.ShowSearch>();
        cut.Find("input").FocusIn(new FocusEventArgs());
        await cut.Find("input").InputAsync("Breaking");
        cut.WaitForAssertion(() => Assert.Contains("Breaking Bad", cut.Markup), TimeSpan.FromSeconds(3));

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        // Selected index is now 1 - ArrowUp must clamp back down to 0, not go negative.
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.EndsWith($"/showlist/show/{firstShowId}", nav.Uri);
    }

    [Fact]
    public async Task HoveringOverAResult_HighlightsIt()
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

        cut.Find("div[style*='cursor: pointer']").MouseOver();

        Assert.Contains("bg-primary", cut.Markup);
    }

    [Fact]
    public async Task LosingFocus_ClosesTheDropdownAfterADelay()
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

        await cut.Find("input").FocusOutAsync(new FocusEventArgs());

        cut.WaitForAssertion(() => Assert.DoesNotContain("cursor: pointer", cut.Markup), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task EscapeKey_ClosesTheDropdown()
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

        cut.Find("input").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.DoesNotContain("Breaking Bad", cut.Markup);
    }

    [Fact]
    public async Task TypingFewerThanTwoCharacters_ShowsNoResults()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Breaking Bad"));
            ctx.SaveChanges();
        }

        var cut = Render<Showlist2026.Web.Components.Shared.ShowSearch>();
        cut.Find("input").FocusIn(new FocusEventArgs());
        await cut.Find("input").InputAsync("B");

        await Task.Delay(400); // past the 300ms debounce
        Assert.DoesNotContain("Breaking Bad", cut.Markup);
    }

    [Fact]
    public async Task NavigatingAway_ClearsSearchTextAndCloseDropdown()
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

        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        await cut.InvokeAsync(() => nav.NavigateTo("/somewhere-else"));

        Assert.DoesNotContain("Breaking Bad", cut.Markup);
        Assert.Equal("", cut.Find("input").GetAttribute("value"));
    }
}
