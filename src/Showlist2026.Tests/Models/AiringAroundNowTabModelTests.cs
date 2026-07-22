using Showlist2026.Models;
using Xunit;

namespace Showlist2026.Tests.Models;

public class AiringAroundNowTabModelTests
{
    [Fact]
    public void TabId_SanitizesNonAlphanumericCharacters()
    {
        var model = new AiringAroundNowTabModel("2026", new List<EpFilter>());
        Assert.Equal("T2026", model._TabId);
    }

    [Fact]
    public void TabId_ReplacesSpacesAndPunctuationWithX()
    {
        // Each non-alphanumeric character is replaced individually (no + quantifier), so two
        // punctuation marks become two "x"s, not one.
        var model = new AiringAroundNowTabModel("2024!!", new List<EpFilter>());
        Assert.Equal("T2024xx", model._TabId);
    }

    [Fact]
    public void EpisodeList_DefaultsToEmpty_WhenNullPassed()
    {
        var model = new AiringAroundNowTabModel("2026", null!);
        Assert.NotNull(model._EpisodeList);
        Assert.Empty(model._EpisodeList);
    }
}
