using Showlist2026.Models;
using Xunit;

namespace Showlist2026.Tests.Models;

public class ExistingFolderMatchTests
{
    [Fact]
    public void RoundTripsEarliestAndLatestEpisode()
    {
        var match = new ExistingFolderMatch
        {
            FolderName = "My Show",
            FullPath = @"D:\TV\My Show",
            FolderDate = new DateTime(2020, 1, 1),
            EarliestEpisode = "S01E01",
            LatestEpisode = "S02E05"
        };

        Assert.Equal("My Show", match.FolderName);
        Assert.Equal(@"D:\TV\My Show", match.FullPath);
        Assert.Equal(new DateTime(2020, 1, 1), match.FolderDate);
        Assert.Equal("S01E01", match.EarliestEpisode);
        Assert.Equal("S02E05", match.LatestEpisode);
    }
}
