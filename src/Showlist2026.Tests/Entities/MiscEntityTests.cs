using Showlist2026.Entities;
using Xunit;

namespace Showlist2026.Tests.Entities;

/// <summary>
/// Plain get/set round-trips for entity nav properties/fields that have no logic of their own -
/// they're only ever populated by EF Core when materializing a real row, which these
/// unit tests don't exercise, so there's nothing to assert beyond "the value comes back out."
/// </summary>
public class MiscEntityTests
{
    [Fact]
    public void FriendShow_ExposesFriendNavigationProperty()
    {
        var friend = new Friend { Name = "Alice" };
        var friendShow = new FriendShow { Friend = friend };

        Assert.Same(friend, friendShow.Friend);
    }

    [Fact]
    public void FriendCopy_RoundTripsIdAndFriendNavigationProperty()
    {
        var friend = new Friend { Name = "Alice" };
        var copy = new FriendCopy { Id = 5, Friend = friend };

        Assert.Equal(5, copy.Id);
        Assert.Same(friend, copy.Friend);
    }

    [Fact]
    public void GenreText_ExposesGenresNavigationProperty()
    {
        var genres = new List<Genre> { new() };
        var genreText = new GenreText { Genres = genres };

        Assert.Same(genres, genreText.Genres);
    }

    [Fact]
    public void ShowLink_ExposesPredecessorNavigationProperty()
    {
        var predecessor = new Show { Id = 1 };
        var link = new ShowLink { PredecessorShow = predecessor };

        Assert.Same(predecessor, link.PredecessorShow);
    }

    [Fact]
    public void Timezone_RoundTripsAllProperties()
    {
        var tz = new Timezone
        {
            Id = 1,
            timezone = "America/New_York",
            UTCOffset = -5,
            UTCDSTOffset = -4,
            countrycode = "US",
            status = "active"
        };

        Assert.Equal(1, tz.Id);
        Assert.Equal("America/New_York", tz.timezone);
        Assert.Equal(-5, tz.UTCOffset);
        Assert.Equal(-4, tz.UTCDSTOffset);
        Assert.Equal("US", tz.countrycode);
        Assert.Equal("active", tz.status);
    }

    [Fact]
    public void TouchFolder_RoundTripsIdNameAndFileDate()
    {
        var date = DateTime.UtcNow;
        var folder = new TouchFolder { Id = 3, Name = "Show Folder", FileDate = date };

        Assert.Equal(3, folder.Id);
        Assert.Equal("Show Folder", folder.Name);
        Assert.Equal(date, folder.FileDate);
    }
}
