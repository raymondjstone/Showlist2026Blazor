using Showlist2026.Entities;
using Xunit;

namespace Showlist2026.Tests.Entities;

public class EpisodeTests
{
    [Theory]
    [InlineData(1, 2, "S01E02")]
    [InlineData(12, 5, "S12E05")]
    [InlineData(1, 105, "S01E105")]
    [InlineData(0, 0, "S00E00")]
    public void EpNumberFormatted_PadsSingleDigitsOnly(long season, long number, string expected)
    {
        var ep = new Episode { season = season, number = number };
        Assert.Equal(expected, ep.EpNumberFormatted);
    }

    [Theory]
    [InlineData(null, 60)]
    [InlineData("", 60)]
    [InlineData("0", 60)]
    [InlineData("-5", 60)]
    [InlineData("abc", 60)]
    [InlineData("45", 45)]
    [InlineData("120", 120)]
    public void RuntimeInMins_FallsBackTo60_WhenMissingOrInvalid(string? runtime, int expected)
    {
        var ep = new Episode { runtime = runtime };
        Assert.Equal(expected, ep.runtimeinmins);
    }

    [Fact]
    public void AiringTime_WithoutShow_ReturnsRawAirDate()
    {
        var airDate = new DateTimeOffset(2024, 1, 1, 20, 0, 0, TimeSpan.Zero);
        var ep = new Episode { AirDateOffset2 = airDate, show = null };

        Assert.Equal(airDate, ep.AiringTime);
    }

    [Fact]
    public void AiringTime_ReturnsMinValue_WhenAirDateMissing()
    {
        var ep = new Episode { AirDateOffset2 = null };
        Assert.Equal(DateTimeOffset.MinValue, ep.AiringTime);
    }

    [Fact]
    public void AiringTime_AppliesNetworkTimezoneOffset()
    {
        // UTCOffset -4 ("US Eastern") means we add 4 hours to the raw UTC airdate.
        var show = new Show
        {
            Networks = new Network { tz = new Timezone { UTCOffset = -4 } }
        };
        var airDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var ep = new Episode { show = show, AirDateOffset2 = airDate };

        Assert.Equal(new DateTimeOffset(2024, 1, 1, 4, 0, 0, TimeSpan.Zero), ep.AiringTime);
    }

    [Fact]
    public void AiringTime_FallsBackToWebNetworkTimezone_WhenNoNetwork()
    {
        var show = new Show
        {
            WebNetworks = new WebNetwork { tz = new Timezone { UTCOffset = 2 } }
        };
        var airDate = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var ep = new Episode { show = show, AirDateOffset2 = airDate };

        Assert.Equal(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero), ep.AiringTime);
    }

    [Fact]
    public void SummaryClean_ReplacesDoubleQuotesWithSingle()
    {
        var ep = new Episode { summary = "He said \"hello\"" };
        Assert.Equal("He said 'hello'", ep.summaryclean);
    }

    [Fact]
    public void SummaryClean_EmptyWhenNull()
    {
        var ep = new Episode { summary = null };
        Assert.Equal("", ep.summaryclean);
    }

    [Fact]
    public void Nameclean_HtmlEncodesName()
    {
        var ep = new Episode { name = "Tom & Jerry" };
        Assert.Equal("Tom &amp; Jerry", ep.nameclean);
    }

    [Fact]
    public void SummaryCleanWithImage_IncludesNameImageAndSummary()
    {
        var ep = new Episode { name = "Pilot", imagemedium = "http://img/x.jpg", summary = "First episode" };
        var html = ep.summarycleanwithimage;

        Assert.Contains("Pilot", html);
        Assert.Contains("http://img/x.jpg", html);
        Assert.Contains("First episode", html);
    }

    [Fact]
    public void MainCountryCode_EmptyWhenNoShow()
    {
        var ep = new Episode { show = null };
        Assert.Equal("", ep.maincountrycode);
    }

    [Fact]
    public void MainCountryCode_UsesNetworkCountry_WhenPresent()
    {
        var show = new Show { Networks = new Network { country = new Country { code = "US" } } };
        var ep = new Episode { show = show };
        Assert.Equal("US", ep.maincountrycode);
    }

    [Fact]
    public void MainCountryCode_FallsBackToWebNetworkCountry_WhenNoNetwork()
    {
        var show = new Show { WebNetworks = new WebNetwork { country = new Country { code = "GB" } } };
        var ep = new Episode { show = show };
        Assert.Equal("GB", ep.maincountrycode);
    }

    [Fact]
    public void MainCountryCode_EmptyWhenNeitherNetworkHasCountry()
    {
        var show = new Show();
        var ep = new Episode { show = show };
        Assert.Equal("", ep.maincountrycode);
    }

    [Fact]
    public void MainCountryCode_FallsThroughToWebNetwork_WhenNetworkHasNoCountry()
    {
        var show = new Show
        {
            Networks = new Network { country = null },
            WebNetworks = new WebNetwork { country = new Country { code = "GB" } }
        };
        var ep = new Episode { show = show };
        Assert.Equal("GB", ep.maincountrycode);
    }

    [Fact]
    public void MainCountryCode_EmptyWhenWebNetworkPresentButHasNoCountry()
    {
        var show = new Show { WebNetworks = new WebNetwork { country = null } };
        var ep = new Episode { show = show };
        Assert.Equal("", ep.maincountrycode);
    }
}
