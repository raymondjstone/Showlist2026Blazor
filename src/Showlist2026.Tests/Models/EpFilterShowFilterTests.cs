using Showlist2026.Entities;
using Showlist2026.Models;
using Xunit;

namespace Showlist2026.Tests.Models;

public class EpFilterShowFilterTests
{
    private static EpFilter MakeEpFilter(Show show, Episode? ep = null)
    {
        ep ??= new Episode { show = show, season = 1, number = 1 };
        return new EpFilter(ep, new List<TVSite>());
    }

    [Fact]
    public void EpFilter_NetworkFilter_ReturnsNegativeOne_WhenShowHasNoNetwork()
    {
        var ef = MakeEpFilter(new Show { Id = 5 });
        Assert.Equal(-1, ef.networkFilter._ItemKey);
    }

    [Fact]
    public void EpFilter_NetworkFilter_UsesNetworkId_WhenPresent()
    {
        var show = new Show { Id = 5, Networks = new Network { Id = 7 } };
        var ef = MakeEpFilter(show);
        Assert.Equal(7, ef.networkFilter._ItemKey);
    }

    [Fact]
    public void EpFilter_WebNetworkFilter_UsesWebNetworkId_WhenPresent()
    {
        var show = new Show { Id = 5, WebNetworks = new WebNetwork { Id = 9 } };
        var ef = MakeEpFilter(show);
        Assert.Equal(9, ef.webnetworkFilter._ItemKey);
    }

    [Fact]
    public void EpFilter_LanguageFilter_UsesLanguageId_WhenPresent()
    {
        var show = new Show { Id = 5, Languages = new Language { Id = 3 } };
        var ef = MakeEpFilter(show);
        Assert.Equal(3, ef.languageFilter._ItemKey);
    }

    [Fact]
    public void EpFilter_TypeFilter_UsesTypeId_WhenPresent()
    {
        var show = new Show { Id = 5, Types = new Showlist2026.Entities.Type { Id = 4 } };
        var ef = MakeEpFilter(show);
        Assert.Equal(4, ef.typeFilter._ItemKey);
    }

    [Fact]
    public void EpFilter_GenreFilter_UsesGivenGenreIdAndInclude()
    {
        var ef = MakeEpFilter(new Show { Id = 5 });
        var genreBtn = ef.genreFilter(11, true);
        Assert.Equal(11, genreBtn._ItemKey);
        Assert.True(genreBtn._ItemStatus);
    }

    [Fact]
    public void EpFilter_CountryFilter_UsesGivenCountryIdAndInclude()
    {
        var ef = MakeEpFilter(new Show { Id = 5 });
        var countryBtn = ef.countryFilter(22, false);
        Assert.Equal(22, countryBtn._ItemKey);
        Assert.False(countryBtn._ItemStatus);
    }

    [Fact]
    public void EpFilter_ShowFilter_ReturnsNull_WhenEpisodeOrShowMissing()
    {
        var withNoShow = new EpFilter(new Episode { show = null }, new List<TVSite>());
        Assert.Null(withNoShow.showFilter);

        var empty = new EpFilter(new List<TVSite>());
        Assert.Null(empty.showFilter);
    }

    [Fact]
    public void EpFilter_ShowFilter_UsesShowId_WhenPresent()
    {
        var ef = MakeEpFilter(new Show { Id = 42 });
        Assert.Equal(42, ef.showFilter!._ItemKey);
    }

    [Fact]
    public void EpFilter_Missed_TrueWhenAiredMoreThanFourDaysAgo()
    {
        var show = new Show { Id = 1 };
        var oldEp = new Episode { show = show, season = 1, number = 1, AirDateOffset2 = DateTimeOffset.UtcNow.AddDays(-10) };
        var recentEp = new Episode { show = show, season = 1, number = 2, AirDateOffset2 = DateTimeOffset.UtcNow.AddDays(-1) };

        Assert.True(new EpFilter(oldEp, new List<TVSite>()).Missed);
        Assert.False(new EpFilter(recentEp, new List<TVSite>()).Missed);
    }

    [Fact]
    public void EpFilter_Missed_FalseWhenNoEpisode()
    {
        Assert.False(new EpFilter(new List<TVSite>()).Missed);
    }

    [Fact]
    public void EpFilter_AlreadyDecidedUpon_TrueWhenEitherSelectedOrIgnored()
    {
        var ef = MakeEpFilter(new Show { Id = 1 });
        Assert.False(ef.AlreadyDecidedUpon);

        ef.Activelyselected = true;
        Assert.True(ef.AlreadyDecidedUpon);

        ef.Activelyselected = false;
        ef.Activelyignored = true;
        Assert.True(ef.AlreadyDecidedUpon);
    }

    // ===== ShowFilter (Coming Soon) =====

    [Fact]
    public void ShowFilter_NetworkFilter_ReturnsNegativeOne_WhenNoNetwork()
    {
        var sf = new ShowFilter(new Show { Id = 1 });
        Assert.Equal(-1, sf.networkFilter._ItemKey);
    }

    [Fact]
    public void ShowFilter_NetworkFilter_UsesNetworkId_WhenPresent()
    {
        var sf = new ShowFilter(new Show { Id = 1, Networks = new Network { Id = 8 } });
        Assert.Equal(8, sf.networkFilter._ItemKey);
    }

    [Fact]
    public void ShowFilter_ShowFilter_ReturnsNull_WhenNoShow()
    {
        Assert.Null(new ShowFilter().showFilter);
    }

    [Fact]
    public void ShowFilter_ShowFilter_UsesShowId()
    {
        var sf = new ShowFilter(new Show { Id = 15 });
        Assert.Equal(15, sf.showFilter!._ItemKey);
    }

    [Fact]
    public void ShowFilter_Missed_AlwaysFalse()
    {
        Assert.False(new ShowFilter(new Show { Id = 1 }).Missed);
    }

    [Fact]
    public void ShowFilter_WebNetworkFilter_UsesWebNetworkId_WhenPresent()
    {
        var sf = new ShowFilter(new Show { Id = 1, WebNetworks = new WebNetwork { Id = 6 } });
        Assert.Equal(6, sf.webnetworkFilter._ItemKey);
    }

    [Fact]
    public void ShowFilter_LanguageFilter_UsesLanguageId_WhenPresent()
    {
        var sf = new ShowFilter(new Show { Id = 1, Languages = new Language { Id = 3 } });
        Assert.Equal(3, sf.languageFilter._ItemKey);
    }

    [Fact]
    public void ShowFilter_TypeFilter_UsesTypeId_WhenPresent()
    {
        var sf = new ShowFilter(new Show { Id = 1, Types = new Showlist2026.Entities.Type { Id = 4 } });
        Assert.Equal(4, sf.typeFilter._ItemKey);
    }

    [Fact]
    public void ShowFilter_GenreFilter_UsesGivenGenreIdAndInclude()
    {
        var sf = new ShowFilter(new Show { Id = 1 });
        var genreBtn = sf.genreFilter(9, false);
        Assert.Equal(9, genreBtn._ItemKey);
        Assert.False(genreBtn._ItemStatus);
    }

    [Fact]
    public void ShowFilter_CountryFilter_UsesGivenCountryIdAndInclude()
    {
        var sf = new ShowFilter(new Show { Id = 1 });
        var countryBtn = sf.countryFilter(13, true);
        Assert.Equal(13, countryBtn._ItemKey);
        Assert.True(countryBtn._ItemStatus);
    }

    [Fact]
    public void ShowFilter_WebNetworkFilter_ReturnsNegativeOne_WhenNoWebNetwork()
    {
        var sf = new ShowFilter(new Show { Id = 1 });
        Assert.Equal(-1, sf.webnetworkFilter._ItemKey);
    }

    [Fact]
    public void ShowFilter_LanguageFilter_ReturnsNegativeOne_WhenNoLanguage()
    {
        var sf = new ShowFilter(new Show { Id = 1 });
        Assert.Equal(-1, sf.languageFilter._ItemKey);
    }

    [Fact]
    public void ShowFilter_TypeFilter_ReturnsNegativeOne_WhenNoType()
    {
        var sf = new ShowFilter(new Show { Id = 1 });
        Assert.Equal(-1, sf.typeFilter._ItemKey);
    }

    [Fact]
    public void EpFilter_WebNetworkFilter_ReturnsNegativeOne_WhenNoWebNetwork()
    {
        var ef = MakeEpFilter(new Show { Id = 5 });
        Assert.Equal(-1, ef.webnetworkFilter._ItemKey);
    }

    [Fact]
    public void EpFilter_LanguageFilter_ReturnsNegativeOne_WhenNoLanguage()
    {
        var ef = MakeEpFilter(new Show { Id = 5 });
        Assert.Equal(-1, ef.languageFilter._ItemKey);
    }

    [Fact]
    public void EpFilter_TypeFilter_ReturnsNegativeOne_WhenNoType()
    {
        var ef = MakeEpFilter(new Show { Id = 5 });
        Assert.Equal(-1, ef.typeFilter._ItemKey);
    }

    [Fact]
    public void ShowFilter_FromEpFilter_CopiesActiveFlags()
    {
        var show = new Show { Id = 1 };
        var ef = MakeEpFilter(show);
        ef.Activelyselected = true;

        var sf = new ShowFilter(ef);

        Assert.Same(show, sf.ep);
        Assert.True(sf.activelyselected);
        Assert.False(sf.activelyignored);
    }
}
