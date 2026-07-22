using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Entities;

public class ShowTests
{
    [Theory]
    [InlineData("Show: A Test!", "Show A Test")]
    [InlineData("Rock & Roll", "Rock and Roll")]
    [InlineData("Simple Name", "Simple Name")]
    [InlineData("What?", "What")]
    [InlineData("Us/Them", "Us Them")]
    public void DefaultFolderName_SanitizesSpecialCharacters(string name, string expected)
    {
        var show = new Show { name = name };
        Assert.Equal(expected, show.DefaultFolderName);
    }

    [Fact]
    public void DefaultFolderName_EmptyWhenNameMissing()
    {
        var show = new Show { name = null };
        Assert.Equal("", show.DefaultFolderName);
    }

    [Theory]
    [InlineData("2020-05-01", 2020, 5, 1)]
    [InlineData("2020-05-01 00:00:00", 2020, 5, 1)]
    public void ShowStart_ParsesPremieredDate(string premiered, int year, int month, int day)
    {
        var show = new Show { premiered = premiered };
        Assert.Equal(new DateTime(year, month, day), show.ShowStart);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-date")]
    public void ShowStart_ReturnsMinValue_WhenUnparseable(string? premiered)
    {
        var show = new Show { premiered = premiered };
        Assert.Equal(DateTime.MinValue, show.ShowStart);
    }

    [Fact]
    public void ShowPageURL_UsesShowlistShowRoute()
    {
        // Locks in the project route contract: /showlist/show/{id}, never the short /show/{id}.
        var show = new Show { Id = 42 };
        Assert.Equal("/showlist/show/42", show.showPageURL);
    }

    [Fact]
    public void Select2ResultName_IncludesYear_WhenShowStartKnown()
    {
        var show = new Show { name = "Tom & Jerry", premiered = "1999-01-01" };
        Assert.Equal("Tom &amp; Jerry (1999)", show.select2ResultName);
    }

    [Fact]
    public void Select2ResultName_OmitsYear_WhenShowStartUnknown()
    {
        var show = new Show { name = "Mystery Show", premiered = null };
        Assert.Equal("Mystery Show", show.select2ResultName);
    }

    [Fact]
    public void NextEpisode_ReturnsEarliestFutureEpisode()
    {
        var show = TestData.NewShow("Test Show");
        var past = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-10));
        var soonest = TestData.NewEpisode(show, 1, 3, DateTimeOffset.UtcNow.AddDays(1));
        var later = TestData.NewEpisode(show, 1, 4, DateTimeOffset.UtcNow.AddDays(5));

        Assert.Equal(soonest, show.nextEpisode);
    }

    [Fact]
    public void NextEpisode_FallsBackToMostRecentPast_WhenNoFutureEpisodes()
    {
        var show = TestData.NewShow("Ended Show");
        var older = TestData.NewEpisode(show, 1, 1, DateTimeOffset.UtcNow.AddDays(-30));
        var mostRecent = TestData.NewEpisode(show, 1, 2, DateTimeOffset.UtcNow.AddDays(-2));

        Assert.Equal(mostRecent, show.nextEpisode);
    }

    [Fact]
    public void NextEpisode_NullWhenNoEpisodes()
    {
        var show = new Show { name = "Empty", Episodes = new List<Episode>() };
        Assert.Null(show.nextEpisode);
    }

    [Fact]
    public void URLSearchTerm_UsesFolderNameOverDefault_AndAppendsCategory()
    {
        var show = new Show { name = "My Show", FolderName = "My.Custom.Folder" };
        Assert.Equal("My Custom Folder?t=5000", show.URLSearchTerm);
    }

    [Fact]
    public void URLSearchTerm_FallsBackToDefaultFolderName_WhenNoFolderNameSet()
    {
        var show = new Show { name = "My Show" };
        Assert.Equal("My Show?t=5000", show.URLSearchTerm);
    }

    [Fact]
    public void URLSearchTermNameOnly_HasNoCategorySuffix()
    {
        var show = new Show { name = "My Show", FolderName = "My.Folder" };
        Assert.Equal("My Folder", show.URLSearchTermNameOnly);
    }

    [Fact]
    public void URLSearchTermGeekSeek_MatchesNameOnlyVariant()
    {
        var show = new Show { name = "My Show", FolderName = "My.Folder" };
        Assert.Equal("My Folder", show.URLSearchTermGeekSeek);
    }

    [Fact]
    public void Nameclean_HtmlEncodesSpecialCharacters()
    {
        var show = new Show { name = "Tom & Jerry <Show>" };
        Assert.Equal("Tom &amp; Jerry &lt;Show&gt;", show.nameclean);
    }

    [Fact]
    public void Nameclean_EmptyWhenNameNull()
    {
        var show = new Show { name = null };
        Assert.Equal("", show.nameclean);
    }

    [Fact]
    public void NamecleanFolderFriendly_StripsColons_FromTheAlreadyHtmlEncodedName()
    {
        // namecleanFolderFriendly strips ' and : from `nameclean` (the HTML-ENCODED name), not
        // from the raw name. Since HtmlEncode turns ' into "&#39;" first, the apostrophe-strip
        // never actually matches anything by the time it runs - only the colon strip does.
        var show = new Show { name = "It's Always: Sunny" };
        Assert.Equal("^It&#39;s Always Sunny", show.namecleanFolderFriendly);
    }

    [Fact]
    public void SummaryClean_ReplacesQuotesWithSingle()
    {
        var show = new Show { summary = "He said \"hi\"" };
        Assert.Equal("He said 'hi'", show.summaryclean);
    }

    [Fact]
    public void SummaryClean_EmptyWhenNull()
    {
        var show = new Show { summary = null };
        Assert.Equal("", show.summaryclean);
    }

    [Fact]
    public void SummaryCleanWithImage_IncludesNameAndImageAndSummary()
    {
        var show = new Show { name = "My Show", imagemed = "http://img/x.jpg", summary = "A summary" };
        var html = show.summarycleanwithimage;

        Assert.Contains("My Show", html);
        Assert.Contains("http://img/x.jpg", html);
        Assert.Contains("A summary", html);
    }
}
