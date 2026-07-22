using Bunit;
using Microsoft.AspNetCore.Components;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Layout;
using Xunit;

namespace Showlist2026.Tests.Components;

public class MainLayoutTests : BlazorTestBase
{
    public MainLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void RendersNavMenuAndBodyContent()
    {
        var cut = Render<MainLayout>(p => p.Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, "Page Content"))));

        Assert.Contains("Page Content", cut.Markup);
        Assert.Contains("Showlist", cut.Markup);
    }
}
