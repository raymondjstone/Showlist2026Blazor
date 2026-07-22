using Showlist2026.Services;
using Xunit;

namespace Showlist2026.Tests.Parsing;

public class EpisodeNameParserTests
{
    [Theory]
    [InlineData("Show.Name.S01E02.720p.mkv", 1, 2)]
    [InlineData("Show.Name.S1E2.mkv", 1, 2)]
    [InlineData("Sherlock.SE01E02.mkv", 1, 2)]
    [InlineData("Show.Name.1x02.mkv", 1, 2)]
    [InlineData("Show.Name.01x02.mkv", 1, 2)]
    [InlineData("Show.Name.102.720p.mkv", 1, 2)]
    [InlineData("Show.Name.1202.mkv", 12, 2)]
    [InlineData("Show.Name.E02.mkv", 1, 2)]
    [InlineData("Show.Name.E2.mkv", 1, 2)]
    [InlineData("Show.Name.Part.2.mkv", 1, 2)]
    [InlineData("Show.Name.Part 02.mkv", 1, 2)]
    public void ParseFirst_ReturnsExpectedSeasonEpisode(string fileName, long season, long episode)
    {
        var result = EpisodeNameParser.ParseFirst(fileName);

        Assert.NotNull(result);
        Assert.Equal(season, result!.Value.season);
        Assert.Equal(episode, result.Value.episode);
    }

    [Theory]
    [InlineData("RandomFile.mkv")]
    [InlineData("Show.Name.2019.mkv")] // pure 4-digit year token must not be mistaken for S/E
    [InlineData("NoNumbersHere.mkv")]
    public void ParseFirst_ReturnsNull_WhenNoRecognizedPattern(string fileName)
    {
        Assert.Null(EpisodeNameParser.ParseFirst(fileName));
    }

    [Fact]
    public void Parse_MultiEpisode_ReturnsAllEpisodesForSameSeason()
    {
        var result = EpisodeNameParser.Parse("Show.Name.S02E05E06E07.mkv");

        Assert.NotNull(result);
        Assert.Equal(new[] { (2L, 5L), (2L, 6L), (2L, 7L) }, result);
    }

    [Fact]
    public void Parse_Range_ExpandsToEveryEpisodeInclusive()
    {
        var result = EpisodeNameParser.Parse("Show.Name.S01E01-E10.mkv");

        Assert.NotNull(result);
        Assert.Equal(10, result!.Count);
        Assert.Equal((1L, 1L), result[0]);
        Assert.Equal((1L, 10L), result[9]);
    }

    [Fact]
    public void Parse_DescendingRange_FallsBackInsteadOfExpanding()
    {
        // epEnd < epStart is not a valid range, so the range branch must not fire;
        // parsing should fall through to the plain S01E10 multi-episode match instead
        // of throwing or producing a nonsensical/huge list.
        var result = EpisodeNameParser.Parse("Show.Name.S01E10-E05.mkv");

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal((1L, 10L), result[0]);
    }

    [Fact]
    public void ParseFirst_ReturnsFirstOfMultiEpisodeMatch()
    {
        var result = EpisodeNameParser.ParseFirst("Show.Name.S03E01E02.mkv");

        Assert.Equal((3L, 1L), result);
    }
}
