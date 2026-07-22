using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ComingSoonForUserTests
{
    [Fact]
    public void ComingSoonForUser_MatchesRealisticIsoFormattedPremieredDates()
    {
        // Regression test for a fixed bug: ComingSoonForUser used to pre-filter shows by
        // checking whether `premiered` CONTAINS "/<year>" (slash-prefixed), e.g.
        // .Contains("/2026") - but TVMaze's actual `premiered` field is ISO-formatted
        // ("2026-05-01"), which never contains a slash. That silently excluded every real show,
        // making the whole method return empty in production. Fixed to check for a leading
        // year instead (StartsWith), which matches the real ISO format.
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Upcoming Show", premiered: DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"));
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.ComingSoonForUser();

        Assert.Contains(results, r => r.ep.name == "Upcoming Show");
    }

    [Fact]
    public void ComingSoonForUser_AppliesWindowAndFilterLogic()
    {
        using var db = new TestDb();
        var premieredStr = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        using (var ctx = db.CreateContext())
        {
            var inWindow = TestData.NewShow("In Window", premiered: premieredStr);
            var excluded = TestData.NewShow("Excluded", wanted: false, premiered: premieredStr);
            var outsideWindow = TestData.NewShow("Outside Window", premiered: DateTime.UtcNow.AddYears(-5).ToString("yyyy-MM-dd"));
            ctx.Shows.AddRange(inWindow, excluded, outsideWindow);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.ComingSoonForUser();

        Assert.Contains(results, r => r.ep.name == "In Window");
        Assert.DoesNotContain(results, r => r.ep.name == "Excluded");
        Assert.DoesNotContain(results, r => r.ep.name == "Outside Window");
    }
}
