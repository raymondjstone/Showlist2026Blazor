using Bunit;
using Flurl.Http.Testing;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class TrendingPageTests : BlazorTestBase
{
    [Fact]
    public void RendersTrendingShows_WithNetworkTypeAndStatusBadge()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new object[]
        {
            new { show = new { id = 1, name = "Show A", type = "Scripted", network = new { name = "AMC" }, image = new { medium = "http://img/a.jpg" }, status = "Running", summary = "<p>About A</p>" } },
        });

        var cut = Render<Trending>();

        Assert.Contains("Show A", cut.Markup);
        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("Scripted", cut.Markup);
        Assert.Contains("bg-success", cut.Markup); // Running -> success badge
        Assert.Contains("About A", cut.Markup);
        Assert.Contains("http://img/a.jpg", cut.Markup);
    }

    [Fact]
    public void RendersLinkToShowDetail_ForAlreadyTrackedShow()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new object[]
        {
            new { show = new { id = 10, name = "Known Show", type = (string?)null, network = (object?)null, image = (object?)null, status = "Ended" } },
        });

        int localId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Known Show", showid: 10);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            localId = show.Id;
        }

        var cut = Render<Trending>();

        Assert.Contains($"/showlist/show/{localId}", cut.Markup);
        Assert.Contains("Tracked", cut.Markup);
        Assert.Contains("bg-secondary", cut.Markup); // Ended -> secondary badge
    }

    [Fact]
    public void RendersPlainName_ForUntrackedShowWithoutLocalId()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new object[]
        {
            new { show = new { id = 20, name = "Unknown Show", type = (string?)null, network = (object?)null, image = (object?)null, status = "To Be Determined" } },
        });

        var cut = Render<Trending>();

        Assert.Contains("Unknown Show", cut.Markup);
        Assert.DoesNotContain("/showlist/show/", cut.Markup);
        Assert.DoesNotContain(">Tracked<", cut.Markup);
        Assert.Contains("bg-warning", cut.Markup); // "to be determined" -> warning badge
    }

    [Fact]
    public void RendersEmptyList_WhenNoShowsReturned()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(Array.Empty<object>());

        var cut = Render<Trending>();

        Assert.Contains("Trending Shows", cut.Markup);
        Assert.DoesNotContain("card-title", cut.Markup);
    }
}
