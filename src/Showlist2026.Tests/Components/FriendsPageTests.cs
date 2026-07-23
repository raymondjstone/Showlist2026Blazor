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

    [Fact]
    public void EditingAFriend_SavesChangesThroughRealService()
    {
        int friendId;
        using (var ctx = Db.CreateContext())
        {
            var friend = new Showlist2026.Entities.Friend { Name = "Alice", Email = "a@x.com", FolderPath = @"D:\A" };
            ctx.Friends.Add(friend);
            ctx.SaveChanges();
            friendId = friend.Id;
        }

        var cut = Render<Friends>();
        cut.Find("button.btn-outline-primary").Click(); // Edit

        cut.FindAll("input.form-control-sm")[0].Change("Alicia");
        cut.FindAll("input.form-control-sm")[1].Change("alicia@example.com");
        cut.FindAll("input.form-control-sm")[2].Change(@"D:\Friends\Alicia");
        cut.Find("button.btn-success").Click(); // Save

        Assert.Contains("Friend updated.", cut.Markup);
        Assert.Contains("Alicia", cut.Markup);
        using var verify = Db.CreateContext();
        Assert.Equal("Alicia", verify.Friends.Find(friendId)!.Name);
    }

    [Fact]
    public void CancellingAnEdit_DiscardsChanges()
    {
        using (var ctx = Db.CreateContext())
        {
            ctx.Friends.Add(new Showlist2026.Entities.Friend { Name = "Alice", Email = "a@x.com", FolderPath = @"D:\A" });
            ctx.SaveChanges();
        }

        var cut = Render<Friends>();
        cut.Find("button.btn-outline-primary").Click(); // Edit
        cut.FindAll("input.form-control-sm")[0].Change("Should Not Save");
        cut.Find("button.btn-secondary").Click(); // Cancel

        Assert.DoesNotContain("Should Not Save", cut.Markup);
        Assert.Contains("Alice", cut.Markup);
    }

    [Fact]
    public void RemovingAnInterestedShow_PersistsThroughRealService()
    {
        int friendId, friendShowId;
        using (var ctx = Db.CreateContext())
        {
            var friend = new Showlist2026.Entities.Friend { Name = "Alice", Email = "a@x.com", FolderPath = @"D:\A" };
            var show = TestData.NewShow("Watched Show");
            TestData.NewEpisode(show, 1, 1, watched: true);
            ctx.Friends.Add(friend);
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            friendId = friend.Id;

            var friendShow = new Showlist2026.Entities.FriendShow { FriendId = friend.Id, ShowId = show.Id };
            ctx.FriendShows.Add(friendShow);
            ctx.SaveChanges();
            friendShowId = friendShow.Id;
        }

        var cut = Render<Friends>();
        Assert.Single(cut.FindAll(".badge.bg-secondary"));

        cut.Find("span[style='cursor:pointer']").Click();

        Assert.Empty(cut.FindAll(".badge.bg-secondary"));
        using var verify = Db.CreateContext();
        Assert.Empty(verify.FriendShows.Where(fs => fs.Id == friendShowId));
    }

    [Fact]
    public void SearchingForAShow_FiltersTheAddShowDropdown()
    {
        using (var ctx = Db.CreateContext())
        {
            var friend = new Showlist2026.Entities.Friend { Name = "Alice", Email = "a@x.com", FolderPath = @"D:\A" };
            var matching = TestData.NewShow("Breaking Bad");
            TestData.NewEpisode(matching, 1, 1, watched: true);
            var other = TestData.NewShow("The Wire");
            TestData.NewEpisode(other, 1, 1, watched: true);
            ctx.Friends.Add(friend);
            ctx.Shows.Add(matching);
            ctx.Shows.Add(other);
            ctx.SaveChanges();
        }

        var cut = Render<Friends>();
        var options = cut.Find("select.form-select-sm").QuerySelectorAll("option");
        Assert.Equal(3, options.Length); // placeholder + 2 shows

        cut.Find("input[placeholder='Search shows...']").Input("Breaking");

        options = cut.Find("select.form-select-sm").QuerySelectorAll("option");
        Assert.Equal(2, options.Length); // placeholder + 1 matching show
        Assert.Contains("Breaking Bad", cut.Find("select.form-select-sm").TextContent);
        Assert.DoesNotContain("The Wire", cut.Find("select.form-select-sm").TextContent);
    }

    [Fact]
    public void RendersRecentCopiesTable_WhenFriendHasCopies()
    {
        using (var ctx = Db.CreateContext())
        {
            var friend = new Showlist2026.Entities.Friend { Name = "Alice", Email = "a@x.com", FolderPath = @"D:\A" };
            ctx.Friends.Add(friend);
            ctx.SaveChanges();
            ctx.FriendCopies.Add(new Showlist2026.Entities.FriendCopy
            {
                FriendId = friend.Id,
                FileName = "Show.S01E01.mkv",
                CopiedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        }

        var cut = Render<Friends>();

        Assert.Contains("Last 1 copied", cut.Markup);
        Assert.Contains("Show.S01E01.mkv", cut.Markup);
    }

    [Fact]
    public void AddingAFriend_WithBlankName_DoesNothing()
    {
        var cut = Render<Friends>();

        cut.Find("button.btn-primary").Click(); // newName left blank

        using var verify = Db.CreateContext();
        Assert.Empty(verify.Friends);
    }
}
