using System.Text.RegularExpressions;

namespace Showlist2026.Services;

public static class EpisodeNameParser
{
    private static readonly Regex SeRegex = new(@"[Ss](?<season>\d{1,4})[Ee](?<episode>\d{1,4})");
    private static readonly Regex XRegex = new(@"(?<season>\d{1,4})[xX](?<episode>\d{1,4})");
    // Bare format: 102 = S1E02, 1202 = S12E02, delimited by . _ - or space
    private static readonly Regex BareRegex = new(@"(?<=[\.\-_ ])(?<season>\d{1,2})(?<episode>\d{2})(?=[\.\-_ ])");

    /// <summary>
    /// Parses season and episode numbers from a filename.
    /// Tries S01E02, then 01x02, then bare 102/1202 (delimited by . _ - or space).
    /// Returns null if no pattern matches.
    /// </summary>
    public static (long season, long episode)? Parse(string fileName)
    {
        var match = SeRegex.Match(fileName);
        if (!match.Success) match = XRegex.Match(fileName);
        if (!match.Success) match = BareRegex.Match(fileName);

        if (!match.Success) return null;

        return (long.Parse(match.Groups["season"].Value), long.Parse(match.Groups["episode"].Value));
    }
}
