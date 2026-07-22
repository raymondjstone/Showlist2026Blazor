using Bunit;
using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class DownloadedShowPageTests : BlazorTestBase
{
    [Fact]
    public void DefaultsToCurrentYear_AndRendersMatchingTouchFiles()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Breaking Bad");
            var ep = TestData.NewEpisode(show, 1, 1);
            ctx.Shows.Add(show);
            ctx.TouchFiles.Add(new TouchFile { Name = "breaking.bad.s01e01.mkv", FileDate = DateTime.Now, Episode = ep });
            ctx.SaveChanges();
        }

        var cut = Render<DownloadedShow>();

        Assert.Contains("Breaking Bad", cut.Markup);
        Assert.Contains("breaking.bad.s01e01.mkv", cut.Markup);
    }

    [Fact]
    public void ChangingYearSelector_ReloadsFilesForThatYear()
    {
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("Old Show");
            var ep = TestData.NewEpisode(show, 1, 1);
            ctx.Shows.Add(show);
            ctx.TouchFiles.Add(new TouchFile { Name = "old.file.mkv", FileDate = new DateTime(2019, 3, 1), Episode = ep });
            ctx.SaveChanges();
        }

        var cut = Render<DownloadedShow>();
        Assert.DoesNotContain("old.file.mkv", cut.Markup);

        cut.Find("select.form-select").Change("2019");

        Assert.Contains("old.file.mkv", cut.Markup);
    }

    [Fact]
    public void YearRouteParameter_LoadsThatYearOnInit()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.TouchFiles.Add(new TouchFile { Name = "from2020.mkv", FileDate = new DateTime(2020, 5, 1) });
            ctx.SaveChanges();
        }

        var cut = Render<DownloadedShow>(p => p.Add(c => c.Year, 2020));

        Assert.Contains("from2020.mkv", cut.Markup);
    }
}
