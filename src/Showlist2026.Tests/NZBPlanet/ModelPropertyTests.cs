using Showlist2026.NZBPlanetApiJSON;
using Xunit;

namespace Showlist2026.Tests.NZBPlanet;

/// <summary>
/// Plain get/set round-trips for the auto-generated NZBPlanet API DTOs. These properties have
/// no logic of their own - they exist purely to be populated by JSON deserialization - so there's
/// nothing to assert beyond "the value that was set comes back out."
/// </summary>
public class ModelPropertyTests
{
    [Fact]
    public void NzBplanetJSON_ExposesAttributesAndChannel()
    {
        var attrs = new NzBplanetAttributes { Version = "2.0" };
        var channel = new Channel { Title = "Results" };
        var root = new NzBplanetJSON { Attributes = attrs, Channel = channel };

        Assert.Same(attrs, root.Attributes);
        Assert.Same(channel, root.Channel);
        Assert.Equal("2.0", root.Attributes.Version);
    }

    [Fact]
    public void Channel_ExposesDescriptionAndLink()
    {
        var link = new System.Uri("http://example.com/rss");
        var channel = new Channel { Description = "Feed description", Link = link };

        Assert.Equal("Feed description", channel.Description);
        Assert.Same(link, channel.Link);
    }

    [Fact]
    public void Image_RoundTripsAllProperties()
    {
        var url = new System.Uri("http://example.com/img.png");
        var link = new System.Uri("http://example.com/");
        var image = new Image { Url = url, Title = "Logo", Link = link, Description = "Site logo" };

        Assert.Same(url, image.Url);
        Assert.Equal("Logo", image.Title);
        Assert.Same(link, image.Link);
        Assert.Equal("Site logo", image.Description);
    }

    [Fact]
    public void Item_ExposesGuidCommentsDescriptionAndEnclosure()
    {
        var guid = new System.Uri("http://example.com/item/1");
        var comments = new System.Uri("http://example.com/item/1#comments");
        var enclosure = new Enclosure();
        var item = new Item { Guid = guid, Comments = comments, Description = "An episode", Enclosure = enclosure };

        Assert.Same(guid, item.Guid);
        Assert.Same(comments, item.Comments);
        Assert.Equal("An episode", item.Description);
        Assert.Same(enclosure, item.Enclosure);
    }

    [Fact]
    public void Enclosure_ExposesAttributes()
    {
        var attrs = new EnclosureAttributes { Url = new System.Uri("http://example.com/file.nzb"), Length = "12345", Type = "application/x-nzb" };
        var enclosure = new Enclosure { Attributes = attrs };

        Assert.Same(attrs, enclosure.Attributes);
        Assert.Equal("http://example.com/file.nzb", enclosure.Attributes.Url.ToString());
        Assert.Equal("12345", enclosure.Attributes.Length);
        Assert.Equal("application/x-nzb", enclosure.Attributes.Type);
    }
}
