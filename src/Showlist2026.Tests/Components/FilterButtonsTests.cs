using Bunit;
using Showlist2026.Models;
using Showlist2026.Web.Components.Shared;
using Xunit;

namespace Showlist2026.Tests.Components;

public class FilterButtonsTests : Bunit.BunitContext
{
    [Fact]
    public void RendersBothIcons_WhenUndecided()
    {
        var model = new FilterButtonsModel("show", null, 1);
        var cut = Render<FilterButtons>(p => p.Add(c => c.Model, model));

        Assert.Contains("fa-check-circle", cut.Markup);
        Assert.Contains("fa-times-circle", cut.Markup);
    }

    [Fact]
    public void RendersNoIcons_WhenModelIsNull()
    {
        var cut = Render<FilterButtons>(p => p.Add(c => c.Model, null));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void RendersNoIcons_WhenItemKeyIsNegative()
    {
        var model = new FilterButtonsModel("show", null, -1);
        var cut = Render<FilterButtons>(p => p.Add(c => c.Model, model));

        Assert.DoesNotContain("fa-check-circle", cut.Markup);
        Assert.DoesNotContain("fa-times-circle", cut.Markup);
    }

    [Fact]
    public void ClickingPlusIcon_InvokesOnFilterChanged_WithIncludeTrue()
    {
        var model = new FilterButtonsModel("network", null, 42);
        (long id, string type, bool? state)? received = null;

        var cut = Render<FilterButtons>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnFilterChanged, args => received = args));

        cut.Find("i.fa-check-circle").Click();

        Assert.Equal((42L, "network", (bool?)true), received);
    }

    [Fact]
    public void ClickingNegativeIcon_InvokesOnFilterChanged_WithIncludeFalse()
    {
        var model = new FilterButtonsModel("genre", null, 7);
        (long id, string type, bool? state)? received = null;

        var cut = Render<FilterButtons>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.OnFilterChanged, args => received = args));

        cut.Find("i.fa-times-circle").Click();

        Assert.Equal((7L, "genre", (bool?)false), received);
    }

    [Fact]
    public void RendersOnlyNegativeIcon_WhenAlreadyIncluded()
    {
        var model = new FilterButtonsModel("show", true, 1);
        var cut = Render<FilterButtons>(p => p.Add(c => c.Model, model));

        Assert.DoesNotContain("fa-check-circle", cut.Markup);
        Assert.Contains("fa-times-circle", cut.Markup);
    }
}
