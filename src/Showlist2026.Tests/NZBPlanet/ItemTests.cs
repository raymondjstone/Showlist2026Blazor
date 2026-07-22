using Showlist2026.NZBPlanetApiJSON;
using Xunit;

namespace Showlist2026.Tests.NZBPlanet;

public class ItemTests
{
    private static Item MakeItem(string category, string title, string? size = null, string? season = null, string? episode = null)
    {
        var attrs = new System.Collections.Generic.List<Attr>();
        if (size != null)
            attrs.Add(new Attr { Attributes = new AttrAttributes { Name = Name.Size, Value = size } });
        if (season != null)
            attrs.Add(new Attr { Attributes = new AttrAttributes { Name = Name.Season, Value = season } });
        if (episode != null)
            attrs.Add(new Attr { Attributes = new AttrAttributes { Name = Name.Episode, Value = episode } });

        return new Item { Category = category, Title = title, Attr = attrs };
    }

    [Fact]
    public void SizeAsNumber_ParsesNumericSizeAttribute()
    {
        var item = MakeItem("TV", "Show.S01E01", size: "12345");
        Assert.Equal(12345, item.SizeAsNumber);
    }

    [Fact]
    public void SizeAsNumber_ReturnsMaxValue_WhenSizeMissingOrUnparseable()
    {
        var item = MakeItem("TV", "Show.S01E01");
        Assert.Equal(int.MaxValue, item.SizeAsNumber);
    }

    [Fact]
    public void SizeAsNumberMBs_ConvertsBytesToMegabytes()
    {
        var item = MakeItem("TV", "Show.S01E01", size: (500 * 1024 * 1024).ToString());
        Assert.Equal(500, item.SizeAsNumberMBs);
    }

    [Fact]
    public void Season_Episode_DefaultToEmptyString_WhenAttributeMissing()
    {
        var item = MakeItem("TV", "Show.S01E01");
        Assert.Equal("", item.Season);
        Assert.Equal("", item.Episode);
    }

    [Fact]
    public void Season_Episode_ReturnAttributeValues_WhenPresent()
    {
        var item = MakeItem("TV", "Show.S01E01", season: "1", episode: "1");
        Assert.Equal("1", item.Season);
        Assert.Equal("1", item.Episode);
    }

    [Fact]
    public void EpNumberFormatted_ConcatenatesSeasonAndEpisode()
    {
        var item = MakeItem("TV", "Show.S01E01", season: "01", episode: "05");
        Assert.Equal("0105", item.EpNumberFormatted);
    }

    [Fact]
    public void Sortkey_ForeignCategory_AddsPenalty()
    {
        // "foreign" bumps the base by 500 before the SD/x265/size tiers apply.
        var foreignSd = MakeItem("Foreign SD", "Show.x264", size: (100 * 1024 * 1024).ToString());
        var domesticSd = MakeItem("SD", "Show.x264", size: (100 * 1024 * 1024).ToString());

        Assert.Equal(domesticSd.Sortkey + 500, foreignSd.Sortkey);
    }

    [Fact]
    public void Sortkey_SdWithX264Title_RanksBestAmongSdReleases()
    {
        var sdX264 = MakeItem("SD", "Show.x264.mkv");
        var sdOther = MakeItem("SD", "Show.mkv");

        Assert.True(sdX264.Sortkey < sdOther.Sortkey);
    }

    [Fact]
    public void Sortkey_NonSdWithX265Title_RanksAboveGenericSize()
    {
        var x265 = MakeItem("HD", "Show.x265.mkv", size: (2000 * 1024 * 1024).ToString());
        var genericLarge = MakeItem("HD", "Show.mkv", size: (2000 * 1024 * 1024).ToString());

        Assert.True(x265.Sortkey < genericLarge.Sortkey);
    }

    [Fact]
    public void Sortkey_SmallNonSdRelease_RanksAboveLargeGenericRelease()
    {
        var small = MakeItem("HD", "Show.mkv", size: (100 * 1024 * 1024).ToString()); // < 501MB
        var large = MakeItem("HD", "Show.mkv", size: (2000 * 1024 * 1024).ToString());

        Assert.True(small.Sortkey < large.Sortkey);
    }

    [Fact]
    public void Sortkey_GenericLargeRelease_RanksBySizeDescendingOrder()
    {
        var smaller = MakeItem("HD", "Show.mkv", size: (1000 * 1024 * 1024).ToString());
        var bigger = MakeItem("HD", "Show.mkv", size: (2000 * 1024 * 1024).ToString());

        // Both fall into the "generic size" tier (SizeAsNumberMBs + base + 10000), so a bigger
        // file sorts with a higher key.
        Assert.True(smaller.Sortkey < bigger.Sortkey);
    }
}
