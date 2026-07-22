using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class ShowListAppServiceMoreFilterTests
{
    [Fact]
    public async Task LanguageFilter_SetsWantedState()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var lang = TestData.NewLanguage("English");
            ctx.Languages.Add(lang);
            ctx.SaveChanges();
            id = lang.Id;
        }

        var service = TestFactory.CreateAppService(db);
        Assert.True(await service.LanguageFilter(id, false));

        using var verify = db.CreateContext();
        Assert.False(verify.Languages.Find(id)!.Wanted);
    }

    [Fact]
    public async Task LanguageFilter_ReturnsFalse_WhenMissing()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        Assert.False(await service.LanguageFilter(999, true));
    }

    [Fact]
    public async Task CountryFilter_SetsWantedState()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var country = TestData.NewCountry("US");
            ctx.Countrys.Add(country);
            ctx.SaveChanges();
            id = country.Id;
        }

        var service = TestFactory.CreateAppService(db);
        Assert.True(await service.CountryFilter(id, true));

        using var verify = db.CreateContext();
        Assert.True(verify.Countrys.Find(id)!.Wanted);
    }

    [Fact]
    public async Task NetworkFilter_SetsWantedState()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var network = TestData.NewNetwork("HBO");
            ctx.Networks.Add(network);
            ctx.SaveChanges();
            id = network.Id;
        }

        var service = TestFactory.CreateAppService(db);
        Assert.True(await service.NetworkFilter(id, false));

        using var verify = db.CreateContext();
        Assert.False(verify.Networks.Find(id)!.Wanted);
    }

    [Fact]
    public async Task WebNetworkFilter_SetsWantedState()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var webNetwork = TestData.NewWebNetwork("Netflix");
            ctx.WebNetworks.Add(webNetwork);
            ctx.SaveChanges();
            id = webNetwork.Id;
        }

        var service = TestFactory.CreateAppService(db);
        Assert.True(await service.WebNetworkFilter(id, true));

        using var verify = db.CreateContext();
        Assert.True(verify.WebNetworks.Find(id)!.Wanted);
    }

    [Fact]
    public async Task TypeFilter_SetsWantedState()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var type = TestData.NewType("Scripted");
            ctx.Types.Add(type);
            ctx.SaveChanges();
            id = type.Id;
        }

        var service = TestFactory.CreateAppService(db);
        Assert.True(await service.TypeFilter(id, false));

        using var verify = db.CreateContext();
        Assert.False(verify.Types.Find(id)!.Wanted);
    }

    [Fact]
    public async Task GenreFilter_SetsWantedState()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var genre = TestData.NewGenreText("Drama");
            ctx.GenreTexts.Add(genre);
            ctx.SaveChanges();
            id = genre.Id;
        }

        var service = TestFactory.CreateAppService(db);
        Assert.True(await service.GenreFilter(id, true));

        using var verify = db.CreateContext();
        Assert.True(verify.GenreTexts.Find(id)!.Wanted);
    }

    [Fact]
    public void GivenUpEpisodes_ReturnsOnlyGivenUpEpisodes()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var show = TestData.NewShow("Show");
            TestData.NewEpisode(show, 1, 1, givenUp: true);
            TestData.NewEpisode(show, 1, 2); // not given up
            ctx.Shows.Add(show);
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.GivenUpEpisodes();

        var result = Assert.Single(results);
        Assert.Equal(1, result.ep.number);
    }

    [Fact]
    public async Task SetShowNotes_UpdatesNotes()
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
        await service.SetShowNotes(showId, "great show");

        using var verify = db.CreateContext();
        Assert.Equal("great show", verify.Shows.Find(showId)!.Notes);
    }

    [Fact]
    public async Task SetShowPriority_UpdatesPriority()
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
        await service.SetShowPriority(showId, 9);

        using var verify = db.CreateContext();
        Assert.Equal(9, verify.Shows.Find(showId)!.Priority);
    }

    [Fact]
    public async Task SetFolderName_UpdatesFolderName()
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
        await service.SetFolderName(showId, "Custom.Folder.Name");

        using var verify = db.CreateContext();
        Assert.Equal("Custom.Folder.Name", verify.Shows.Find(showId)!.FolderName);
    }

    [Fact]
    public async Task TVSiteUpdate_CreatesNewSite_WhenIdIsZero()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        await service.TVSiteUpdate(0, true, 1, "MySite", "http://example.com/{term}", apiKey: "key123");

        using var verify = db.CreateContext();
        var site = Assert.Single(verify.TVSites);
        Assert.Equal("MySite", site.Name);
        Assert.True(site.Active);
        Assert.Equal("key123", site.ApiKey);
    }

    [Fact]
    public async Task TVSiteUpdate_UpdatesExistingSite_WhenIdMatches()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var newSite = new Showlist2026.Entities.TVSite { Name = "Old", Order = 1, Active = false, URLTemplate = "old" };
            ctx.TVSites.Add(newSite);
            ctx.SaveChanges();
            id = newSite.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.TVSiteUpdate(id, true, 2, "New", "new-template");

        using var verify = db.CreateContext();
        Assert.Single(verify.TVSites);
        var updatedSite = verify.TVSites.Find(id)!;
        Assert.Equal("New", updatedSite.Name);
        Assert.True(updatedSite.Active);
    }

    [Fact]
    public async Task TVSiteDelete_RemovesSite()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var site = new Showlist2026.Entities.TVSite { Name = "Site", Order = 1 };
            ctx.TVSites.Add(site);
            ctx.SaveChanges();
            id = site.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.TVSiteDelete(id);

        using var verify = db.CreateContext();
        Assert.Empty(verify.TVSites);
    }

    [Fact]
    public async Task TVDirectoryUpdate_CreatesAndUpdatesDirectory()
    {
        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        await service.TVDirectoryUpdate(0, @"D:\TV", daysToScan: 7, filter: "*.*", minFileSize: 1000, aliasable: true);

        var dirs = service.TvDirectories();
        var dir = Assert.Single(dirs);
        Assert.Equal(@"D:\TV", dir.Name);
        Assert.True(dir.Aliasable);

        await service.TVDirectoryUpdate(dir.Id, @"D:\TV2", daysToScan: 14, filter: "*.mkv", minFileSize: 2000);

        var updated = service.TvDirectories().Single();
        Assert.Equal(@"D:\TV2", updated.Name);
        Assert.Equal(14, updated.DaysToScan);
    }

    [Fact]
    public async Task TVDirectoryDelete_RemovesDirectory()
    {
        using var db = new TestDb();
        int id;
        using (var ctx = db.CreateContext())
        {
            var dir = new Showlist2026.Entities.TVDirectories { Name = @"D:\TV" };
            ctx.TVDirectories.Add(dir);
            ctx.SaveChanges();
            id = dir.Id;
        }

        var service = TestFactory.CreateAppService(db);
        await service.TVDirectoryDelete(id);

        Assert.Empty(service.TvDirectories());
    }

    [Fact]
    public async Task CheckNewSeasonNotifications_NotifiesOnlyForWantedShowsWithRecentSeasonPremiere()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            var wanted = TestData.NewShow("Wanted Show", wanted: true);
            TestData.NewEpisode(wanted, 2, 1, DateTimeOffset.UtcNow.AddHours(-1)); // recent season premiere

            var undecidedShow = TestData.NewShow("Undecided Show"); // undecided -> not notified
            TestData.NewEpisode(undecidedShow, 2, 1, DateTimeOffset.UtcNow.AddHours(-1));

            var wrongEpisode = TestData.NewShow("Wanted But Not Premiere", wanted: true);
            TestData.NewEpisode(wrongEpisode, 2, 3, DateTimeOffset.UtcNow.AddHours(-1)); // not episode 1

            ctx.Shows.AddRange(wanted, undecidedShow, wrongEpisode);
            ctx.SaveChanges();
        }

        var notifications = new FakeNotificationService();
        var service = TestFactory.CreateAppService(db, notifications: notifications);

        await service.CheckNewSeasonNotifications();

        var sent = Assert.Single(notifications.Sent);
        Assert.Contains("Wanted Show", sent.Title);
    }

    [Fact]
    public async Task GetRecentCopiesForFriend_ReturnsMostRecentFirst_LimitedByCount()
    {
        using var db = new TestDb();
        int friendId;
        using (var ctx = db.CreateContext())
        {
            var friend = new Showlist2026.Entities.Friend { Name = "Alice", Email = "a@example.com", FolderPath = @"D:\A" };
            ctx.Friends.Add(friend);
            ctx.SaveChanges();
            friendId = friend.Id;

            ctx.FriendCopies.Add(new Showlist2026.Entities.FriendCopy { FriendId = friendId, FileName = "old.mkv", CopiedAt = DateTime.UtcNow.AddDays(-2) });
            ctx.FriendCopies.Add(new Showlist2026.Entities.FriendCopy { FriendId = friendId, FileName = "new.mkv", CopiedAt = DateTime.UtcNow });
            ctx.SaveChanges();
        }

        var service = TestFactory.CreateAppService(db);
        var results = service.GetRecentCopiesForFriend(friendId, count: 1);

        var result = Assert.Single(results);
        Assert.Equal("new.mkv", result.FileName);
    }
}
