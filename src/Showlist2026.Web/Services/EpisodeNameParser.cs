using System.Text.RegularExpressions;

namespace Showlist2026.Services;

public static class EpisodeNameParser
{
    // S01E02, S01E02E03, S01E02E03E04, SE01E02 (SHERLOCK-style), etc.
    private static readonly Regex SeMultiRegex = new(@"[Ss][Ee]?(?<season>\d{1,4})[Ee](?<episode>\d{1,4})(?:[Ee](?<extra>\d{1,4}))*");
    // S01E01-E10 range
    private static readonly Regex SeRangeRegex = new(@"[Ss][Ee]?(?<season>\d{1,4})[Ee](?<epStart>\d{1,4})\s*-\s*[Ee](?<epEnd>\d{1,4})");
    // 01x02
    private static readonly Regex XRegex = new(@"(?<season>\d{1,4})[xX](?<episode>\d{1,4})");
    // Bare format: 102 = S1E02, 1202 = S12E02, delimited by . _ - or space
    private static readonly Regex BareRegex = new(@"(?<=[\.\-_ ])(?<season>\d{1,2})(?<episode>\d{2})(?=[\.\-_ ])");
    // No-season patterns: E02, E2 (no preceding S## or SE##)
    private static readonly Regex BareEpisodeRegex = new(@"(?<![Ss][Ee]?\d{1,4})[Ee](?<episode>\d{1,4})");
    // Part 2, Part 02
    private static readonly Regex PartRegex = new(@"(?:^|[\.\-_ ])Part[\s\.\-_]*(?<episode>\d{1,4})(?:$|[\.\-_ ])", RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses season and episode numbers from a filename.
    /// Returns a list of (season, episode) tuples for multi-episode files.
    /// Returns null if no pattern matches.
    /// </summary>
    public static IReadOnlyList<(long season, long episode)>? Parse(string fileName)
    {
        // Try range first (S01E01-E10) since it's more specific than multi
        var rangeMatch = SeRangeRegex.Match(fileName);
        if (rangeMatch.Success)
        {
            var season = long.Parse(rangeMatch.Groups["season"].Value);
            var epStart = long.Parse(rangeMatch.Groups["epStart"].Value);
            var epEnd = long.Parse(rangeMatch.Groups["epEnd"].Value);
            if (epEnd >= epStart)
            {
                var results = new List<(long, long)>();
                for (long ep = epStart; ep <= epEnd; ep++)
                    results.Add((season, ep));
                return results;
            }
        }

        // Try S01E02E03E04 multi-episode
        var seMatch = SeMultiRegex.Match(fileName);
        if (seMatch.Success)
        {
            var season = long.Parse(seMatch.Groups["season"].Value);
            var results = new List<(long, long)>
            {
                (season, long.Parse(seMatch.Groups["episode"].Value))
            };
            foreach (Capture capture in seMatch.Groups["extra"].Captures)
            {
                results.Add((season, long.Parse(capture.Value)));
            }
            return results;
        }

        // Try 01x02
        var xMatch = XRegex.Match(fileName);
        if (xMatch.Success)
        {
            return new[] { (long.Parse(xMatch.Groups["season"].Value), long.Parse(xMatch.Groups["episode"].Value)) };
        }

        // Try bare format (102, 1202) with year exclusion
        var bareMatch = BareRegex.Match(fileName);
        while (bareMatch.Success)
        {
            if (int.TryParse(bareMatch.Value, out var num) && num >= 1900 && num <= 2099)
            {
                bareMatch = bareMatch.NextMatch();
                continue;
            }
            return new[] { (long.Parse(bareMatch.Groups["season"].Value), long.Parse(bareMatch.Groups["episode"].Value)) };
        }

        // Try E02 / E2 (no season, assume season 1)
        var bareEpMatch = BareEpisodeRegex.Match(fileName);
        if (bareEpMatch.Success)
        {
            return new[] { (1L, long.Parse(bareEpMatch.Groups["episode"].Value)) };
        }

        // Try Part 2 / Part 02 (no season, assume season 1)
        var partMatch = PartRegex.Match(fileName);
        if (partMatch.Success)
        {
            return new[] { (1L, long.Parse(partMatch.Groups["episode"].Value)) };
        }

        return null;
    }

    /// <summary>
    /// Convenience method: returns just the first (season, episode) or null.
    /// Use this when only a single episode match is needed.
    /// </summary>
    public static (long season, long episode)? ParseFirst(string fileName)
    {
        var results = Parse(fileName);
        return results is { Count: > 0 } ? results[0] : null;
    }
}
