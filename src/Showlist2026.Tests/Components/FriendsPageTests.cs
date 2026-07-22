using Bunit;
using Showlist2026.Tests.TestInfrastructure;
using Showlist2026.Web.Components.Pages;
using Xunit;

namespace Showlist2026.Tests.Components;

public class FriendsPageTests : BlazorTestBase
{
    [Fact]
    public void AddingAFriend_PersistsThroughRealServiceAndRendersIt()
    {
        var cut = Render<Friends>();

        cut.FindAll("input.form-control")[0].Change("Alice"); // newName
        cut.FindAll("input.form-control")[1].Change("alice@example.com"); // newEmail
        cut.FindAll("input.form-control")[2].Change(@"D:\Friends\Alice"); // newFolderPath
        cut.Find("button.btn-primary").Click();

        Assert.Contains("Alice", cut.Markup);
        Assert.Contains("alice@example.com", cut.Markup);
        using var verify = Db.CreateContext();
        Assert.Single(verify.Friends);
    }

    [Fact]
    public void DeletingAFriend_RemovesThemThroughRealService()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Friends.Add(new Showlist2026.Entities.Friend { Name = "Alice", Email = "a@x.com", FolderPath = @"D:\A" });
            ctx.SaveChanges();
        }

        var cut = Render<Friends>();
        Assert.Contains("Alice", cut.Markup);

        cut.Find("button.btn-danger").Click();

        Assert.DoesNotContain("Alice", cut.Markup);
        using var verify = Db.CreateContext();
        Assert.Empty(verify.Friends);
    }

    [Fact]
    public void AddingAnInterestedShow_PersistsThroughRealServiceAndRenders()
    {
        int friendId, showId;
        using (var ctx = Db.CreateContext())
        {
            var friend = new Showlist2026.Entities.Friend { Name = "Alice", Email = "a@x.com", FolderPath = @"D:\A" };
            var show = TestData.NewShow("Watched Show");
            Showlist2026.Tests.TestInfrastructure.TestData.NewEpisode(show, 1, 1, watched: true);
            ctx.Friends.Add(friend);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            friendId = friend.Id;
            showId = show.Id;
        }

        var cut = Render<Friends>();
        cut.Find("select.form-select-sm").Change(showId.ToString());
        cut.Find("button.btn-outline-success").Click();

        Assert.Contains("Watched Show", cut.Markup);
        using var verify = Db.CreateContext();
        Assert.Single(verify.FriendShows.Where(fs => fs.FriendId == friendId && fs.ShowId == showId));
    }
}
