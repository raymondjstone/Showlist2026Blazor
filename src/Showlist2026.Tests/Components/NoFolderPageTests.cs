using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class NoFolderPageTests : BlazorTestBase
{
    [Fact]
    public void RendersOnlyWantedShowsMissingFolderName()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Missing Folder", wanted: true));
            ctx.Shows.Add(TestData.NewShow("Has Folder", wanted: true, folderName: "Has.Folder"));
            ctx.Shows.Add(TestData.NewShow("Not Wanted"));
            ctx.SaveChanges();
        }

        var cut = Render<NoFolder>();

        Assert.Contains("Missing Folder", cut.Markup);
        Assert.DoesNotContain("Has Folder", cut.Markup);
        Assert.DoesNotContain("Not Wanted", cut.Markup);
    }

    [Fact]
    public void ClickingUseDefaultName_SetsFolderNameThroughRealService_AndRemovesFromList()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<NoFolder>();
        cut.Find("button.btn-outline-success").Click();

        using var verify = Db.CreateContext();
        Assert.Equal("My Show", verify.Shows.Find(showId)!.FolderName);
        Assert.Contains("Shows Without Folder Names (0)", cut.Markup);
    }

    [Fact]
    public void RendersStatusBadge()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("My Show", wanted: true, status: "Running"));
            ctx.SaveChanges();
        }

        var cut = Render<NoFolder>();

        Assert.Contains("bg-success\">Running", cut.Markup);
    }

    [Fact]
    public void UsingCustomFolderName_OpensModalAndPersistsThroughRealService()
    {
        int showId;
        using (var ctx = Db.CreateContext())
        {
            var show = TestData.NewShow("My Show", wanted: true);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var cut = Render<NoFolder>();
        cut.Find("button.btn-outline-primary").Click(); // "Custom"

        Assert.Contains("Save Folder Name", cut.Markup);

        cut.Find(".modal-body input.form-control").Change("Custom Folder Name");
        cut.Find("button.btn-primary:not(.btn-sm)").Click(); // "Save" in modal footer

        using var verify = Db.CreateContext();
        Assert.Equal("Custom Folder Name", verify.Shows.Find(showId)!.FolderName);
        Assert.DoesNotContain("Save Folder Name", cut.Markup);
    }
}
