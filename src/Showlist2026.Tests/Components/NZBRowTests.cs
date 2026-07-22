using Bunit;
using Showlist2026.NZBPlanetApiJSON;
using Xunit;

namespace Showlist2026.Tests.Components;

public class NZBRowTests : BunitContext
{
    [Fact]
    public void RendersItemTitleLinkAndFormattedSize()
    {
        var item = new Item
        {
            Title = "Breaking.Bad.S01E01.mkv",
            Link = new Uri("http://example.com/nzb/1"),
            PubDate = "2008-01-20",
            Category = "TV",
            Attr = new List<Attr>
            {
                new Attr { Attributes = new AttrAttributes { Name = Name.Size, Value = (100 * 1024 * 1024).ToString() } },
                new Attr { Attributes = new AttrAttributes { Name = Name.Season, Value = "S01" } },
                new Attr { Attributes = new AttrAttributes { Name = Name.Episode, Value = "E01" } },
            }
        };

        var cut = Render<Showlist2026.Web.Components.Shared.NZBRow>(p => p.Add(c => c.Item, item));

        Assert.Contains("Breaking.Bad.S01E01.mkv", cut.Markup);
        Assert.Contains("100MB", cut.Markup);
        Assert.Contains("S01E01", cut.Markup);
        Assert.Contains("http://example.com/nzb/1", cut.Markup);
    }

    [Fact]
    public void RendersNothing_WhenItemIsNull()
    {
        var cut = Render<Showlist2026.Web.Components.Shared.NZBRow>();

        Assert.Equal("", cut.Markup.Trim());
    }
}
