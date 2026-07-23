using Bunit;
using Flurl.Http.Testing;
using System.Linq;
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

    [Fact]
    public void SearchResults_RenderNetworkWebNetworkAndWantedBadges()
    {
        using (var ctx = Db.CreateContext())
        {
            var country = TestData.NewCountry("US");
            var wanted = TestData.NewShow("Wanted Show", wanted: true, status: "Running",
                network: TestData.NewNetwork("AMC", country: country));
            var excluded = TestData.NewShow("Excluded Show", wanted: false, status: "Ended",
                webNetwork: TestData.NewWebNetwork("Netflix"));
            var undecided = TestData.NewShow("Undecided Show", status: "To Be Determined");
            ctx.Shows.AddRange(wanted, excluded, undecided);
            ctx.SaveChanges();
        }

        var cut = Render<AdvancedSearch>();
        cut.Find("button.btn-primary").Click();

        Assert.Contains("AMC", cut.Markup);
        Assert.Contains("Netflix", cut.Markup);
        Assert.Contains("Wanted</span>", cut.Markup);
        Assert.Contains("Excluded</span>", cut.Markup);
        Assert.Contains("Undecided</span>", cut.Markup);
        Assert.Contains("bg-success\">Running", cut.Markup);
        Assert.Contains("bg-warning text-dark\">To Be Determined", cut.Markup);
    }

    [Fact]
    public void SearchResults_PaginateAcrossMultiplePages()
    {
        using (var ctx = Db.CreateContext())
        {
            for (int i = 1; i <= 55; i++)
                ctx.Shows.Add(TestData.NewShow($"Show {i:D2}"));
            ctx.SaveChanges();
        }

        var cut = Render<AdvancedSearch>();
        cut.Find("button.btn-primary").Click();

        Assert.Contains("Page 1 of 2", cut.Markup);
        Assert.Contains("Local Results (55)", cut.Markup);

        cut.FindAll("li.page-item button.page-link").First(b => b.TextContent.Trim() == "2").Click();

        Assert.Contains("Page 2 of 2", cut.Markup);
    }

    [Fact]
    public async Task SearchingTvMaze_ShowsResultsAndFlagsShowsAlreadyInDb()
    {
        int existingShowId;
        using (var ctx = Db.CreateContext())
        {
            var existing = TestData.NewShow("Breaking Bad", showid: 1);
            ctx.Shows.Add(existing);
            ctx.SaveChanges();
            existingShowId = existing.Id;
        }

        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new[]
        {
            new
            {
                score = 5.0,
                show = new { id = 1, name = "Breaking Bad", type = "Scripted", language = "English", status = "Ended", premiered = "2008-01-20" }
            },
            new
            {
                score = 3.0,
                show = new { id = 2, name = "New Show", type = "Scripted", language = "English", status = "Running", premiered = "2024-01-01" }
            }
        });

        var cut = Render<AdvancedSearch>();
        await cut.Find("input[placeholder='Search TVMaze...']").ChangeAsync("Breaking Bad");
        await cut.Find("button.btn-secondary").ClickAsync(new());

        Assert.Contains("TVMaze Results (2)", cut.Markup);
        Assert.Contains($"/showlist/show/{existingShowId}", cut.Markup);
        Assert.Contains("New Show", cut.Markup);
        Assert.Contains("bg-success\">Yes", cut.Markup);
        Assert.Contains("bg-secondary\">No", cut.Markup);
    }
}
