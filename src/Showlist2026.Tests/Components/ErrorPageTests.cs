using Bunit;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class ErrorPageTests : BunitContext
{
    [Fact]
    public void RendersErrorMessage_WithoutRequestId_WhenNoActivityOrHttpContext()
    {
        var cut = Render<Error>();

        Assert.Contains("An error occurred while processing your request", cut.Markup);
        Assert.DoesNotContain("Request ID", cut.Markup);
    }
}
