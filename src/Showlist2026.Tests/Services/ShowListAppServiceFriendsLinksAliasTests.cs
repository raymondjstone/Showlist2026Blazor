using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListAppServiceFriendsLinksAliasTests
{
    [Fact]
    public async Task AddFriend_TrimsFields()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var friend = await service.AddFriend("  Alice  ", " alice@example.com ", @"  D:\Friends\Alice  ");

        Assert.Equal("Alice", friend.Name);
        Assert.Equal("alice@example.com", friend.Email);
        Assert.Equal(@"D:\Friends\Alice", friend.FolderPath);
    }

    [Fact]
    public async Task UpdateFriend_UpdatesFieldsById()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        var friend = await service.AddFriend("Alice", "a@example.com", @"D:\A");

        await service.UpdateFriend(friend.Id, "Alice B", "ab@example.com", @"D:\B");

        var friends = service.GetFriends();
        var updated = Assert.Single(friends);
        Assert.Equal("Alice B", updated.Name);
        Assert.Equal("ab@example.com", updated.Email);
        Assert.Equal(@"D:\B", updated.FolderPath);
    }

    [Fact]
    public async Task DeleteFriend_CascadesFriendShowsAndFriendCopies()
    {
        using var db = new TestDb();
        int friendId, showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var friend = await service.AddFriend("Alice", "a@example.com", @"D:\A");
        friendId = friend.Id;
        await service.AddFriendShow(friendId, showId);

        using (var ctx = db.CreateContext())
        {
            ctx.FriendCopies.Add(new Showlist2026.Entities.FriendCopy { FriendId = friendId, FileName = "ep1.mkv", CopiedAt = DateTime.UtcNow });
            ctx.SaveChanges();
        }

        await service.DeleteFriend(friendId);

        using var verify = db.CreateContext();
        Assert.Null(verify.Friends.Find(friendId));
        Assert.Empty(verify.FriendShows.Where(fs => fs.FriendId == friendId));
        Assert.Empty(verify.FriendCopies.Where(c => c.FriendId == friendId));
    }

    [Fact]
    public async Task AddFriendShow_DoesNotDuplicate()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        var friend = await service.AddFriend("Alice", "a@example.com", @"D:\A");

        await service.AddFriendShow(friend.Id, showId);
        await service.AddFriendShow(friend.Id, showId); // duplicate call, should be a no-op

        var friends = service.GetFriends();
        Assert.Single(friends.Single().InterestedShows);
    }

    [Fact]
    public async Task RemoveFriendShow_RemovesJustThatAssociation()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.Shows.Add(TestData.NewShow("Show A"));
            ctx.Shows.Add(TestData.NewShow("Show B"));
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var friend = await service.AddFriend("Alice", "a@example.com", @"D:\A");

        int showAId, showBId;
        using (var ctx = db.CreateContext())
        {
            showAId = ctx.Shows.Single(s => s.name == "Show A").Id;
            showBId = ctx.Shows.Single(s => s.name == "Show B").Id;
        }

        await service.AddFriendShow(friend.Id, showAId);
        await service.AddFriendShow(friend.Id, showBId);

        int friendShowIdForA;
        using (var ctx = db.CreateContext())
            friendShowIdForA = ctx.FriendShows.Single(fs => fs.ShowId == showAId).Id;

        await service.RemoveFriendShow(friendShowIdForA);

        var remaining = service.GetFriends().Single().InterestedShows;
        Assert.Single(remaining);
        Assert.Equal(showBId, remaining[0].ShowId);
    }

    [Fact]
    public void GetWatchedShows_ReturnsOnlyShowsWithAtLeastOneWatchedEpisode()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var watched = TestData.NewShow("Watched Show");
            TestData.NewEpisode(watched, 1, 1, watched: true);

            var unwatched = TestData.NewShow("Unwatched Show");
            TestData.NewEpisode(unwatched, 1, 1);

            ctx.Shows.AddRange(watched, unwatched);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.GetWatchedShows();

        var result = Assert.Single(results);
        Assert.Equal("Watched Show", result.name);
    }

    [Fact]
    public async Task FolderAlias_AddUpdateAndRemove()
    {
        using var db = new TestDb();
        int showId;
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            ctx.Shows.Add(show);
            ctx.SaveChanges();
            showId = show.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.AddFolderAlias(showId, "  Old Show Name  ", seasonOffset: 0);

        var aliases = service.GetFolderAliases(showId);
        var alias = Assert.Single(aliases);
        Assert.Equal("Old Show Name", alias.AliasName); // trimmed
        Assert.Equal(0, alias.SeasonOffset);

        // Adding the same alias name again updates the season offset in place rather than duplicating.
        await service.AddFolderAlias(showId, "Old Show Name", seasonOffset: 3);
        var aliasesAfterUpdate = service.GetFolderAliases(showId);
        var updated = Assert.Single(aliasesAfterUpdate);
        Assert.Equal(3, updated.SeasonOffset);

        await service.RemoveFolderAlias(updated.Id);
        Assert.Empty(service.GetFolderAliases(showId));
    }

    [Fact]
    public async Task ShowLink_AddIsBidirectionalDeduplicated()
    {
        using var db = new TestDb();
        int idA, idB;
        using (var ctx = db.CreateContext())
        {
            var showA = TestData.NewShow("Show A");
            var showB = TestData.NewShow("Show B");
            ctx.Shows.AddRange(showA, showB);
            ctx.SaveChanges();
            idA = showA.Id;
            idB = showB.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.AddShowLink(idA, idB);
        await service.AddShowLink(idB, idA); // reverse direction of the same pair - must not duplicate

        var linksForA = service.GetShowLinks(idA);
        var linksForB = service.GetShowLinks(idB);

        Assert.Single(linksForA);
        Assert.Single(linksForB);
    }

    [Fact]
    public async Task ShowLink_Remove()
    {
        using var db = new TestDb();
        int idA, idB;
        using (var ctx = db.CreateContext())
        {
            var showA = TestData.NewShow("Show A");
            var showB = TestData.NewShow("Show B");
            ctx.Shows.AddRange(showA, showB);
            ctx.SaveChanges();
            idA = showA.Id;
            idB = showB.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.AddShowLink(idA, idB);
        var linkId = service.GetShowLinks(idA).Single().Id;

        await service.RemoveShowLink(linkId);

        Assert.Empty(service.GetShowLinks(idA));
    }
}
