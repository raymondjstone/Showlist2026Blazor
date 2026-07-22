using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class AdminTVDirectoriesPageTests : BlazorTestBase
{
    [Fact]
    public void AddingADirectory_PersistsThroughRealService()
    {
        var cut = Render<AdminTVDirectories>();

        cut.Find("input[placeholder='Directory path']").Change(@"D:\TV");
        cut.Find("button.btn-success").Click();

        using var verify = Db.CreateContext();
        var dir = Assert.Single(verify.TVDirectories);
        Assert.Equal(@"D:\TV", dir.Name);
        Assert.Equal(7, dir.DaysToScan); // default
    }

    [Fact]
    public void DeletingADirectory_RemovesItThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var dir = new Showlist2026.Entities.TVDirectories { Name = @"D:\TV", DaysToScan = 7 };
            ctx.TVDirectories.Add(dir);
            ctx.SaveChanges();
            id = dir.Id;
        }

        var cut = Render<AdminTVDirectories>();
        cut.Find("button.btn-danger").Click();

        using var verify = Db.CreateContext();
        Assert.Null(verify.TVDirectories.Find(id));
    }
}
