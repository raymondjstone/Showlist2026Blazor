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

    [Fact]
    public void EditingAnExistingDirectory_PersistsAllFieldsThroughRealService()
    {
        int id;
        using (var ctx = Db.CreateContext())
        {
            var dir = new Showlist2026.Entities.TVDirectories
            {
                Name = @"D:\Old", DaysToScan = 7, Filter = "*.*", MinFileSize = 50000, Aliasable = false
            };
            ctx.TVDirectories.Add(dir);
            ctx.SaveChanges();
            id = dir.Id;
        }

        var cut = Render<AdminTVDirectories>();

        cut.FindAll("input.form-control-sm")[0].Change(@"D:\New");
        cut.FindAll("input.form-control-sm")[1].Change("14");
        cut.FindAll("input.form-control-sm")[2].Change("*.mkv");
        cut.FindAll("input.form-control-sm")[3].Change("100000");
        cut.Find("input.form-check-input").Change(true);
        cut.Find("button.btn-primary.btn-sm").Click(); // Save

        Assert.Contains(@"Saved: D:\New", cut.Markup);

        using var verify = Db.CreateContext();
        var updated = verify.TVDirectories.Find(id)!;
        Assert.Equal(@"D:\New", updated.Name);
        Assert.Equal(14, updated.DaysToScan);
        Assert.Equal("*.mkv", updated.Filter);
        Assert.Equal(100000, updated.MinFileSize);
        Assert.True(updated.Aliasable);
    }
}
