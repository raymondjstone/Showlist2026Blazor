using Flurl.Http.Testing;
using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Services;

public class NZBPlanetSearchTests
{
    // Shaped like a real NZBPlanet/Newznab API response (note the "@attributes" keys).
    private const string SampleResponseJson = """
    {
      "channel": {
        "title": "NZBPlanet",
        "item": [
          {
            "title": "Show.Name.S01E01.mkv",
            "pubDate": "Mon, 01 Jan 2024 00:00:00 +0000",
            "category": "TV SD",
            "attr": [
              { "@attributes": { "name": "season", "value": "1" } },
              { "@attributes": { "name": "episode", "value": "1" } },
              { "@attributes": { "name": "size", "value": "500000000" } }
            ]
          }
        ]
      }
    }
    """;

    [Fact]
    public async Task NZBPlanetSearch_ReturnsResults_UsingTvMazeIdWhenAvailable()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith(SampleResponseJson);

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);
        var show = new Show { showid = 12345 };

        var result = await service.NZBPlanetSearch(show);

        Assert.NotNull(result);
        httpTest.ShouldHaveCalled("*tvmazeid=12345*");
        var item = Assert.Single(result!.Channel.Item);
        Assert.Equal("Show.Name.S01E01.mkv", item.Title);
    }

    [Fact]
    public async Task NZBPlanetSearch_PopulatesAttributesAndSeasonEpisodeSize()
    {
        // Regression test for a fixed bug: NzBplanetJSON and its nested types were annotated
        // with only Newtonsoft's [JsonProperty], including keys like "@attributes" (not a valid
        // C# identifier, so no matching property name exists at all). Flurl.Http 4.x
        // deserializes with System.Text.Json by default (no Newtonsoft configuration is wired
        // up anywhere in this project), which ignores [JsonProperty] entirely and could never
        // match "@attributes" to the `Attributes` property - Item.Attr[i].Attributes was always
        // null, cascading to Size/Season/Episode/Sortkey/EpNumberFormatted all being
        // empty/default regardless of what the API returned. Fixed by adding
        // [JsonPropertyName]/[JsonConverter] attributes for the System.Text.Json path
        // alongside the existing Newtonsoft ones.
        using var httpTest = new HttpTest();
        httpTest.RespondWith(SampleResponseJson);

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var result = await service.NZBPlanetSearch(new Show { showid = 1 });

        var item = result!.Channel.Item.Single();
        Assert.NotEmpty(item.Attr);
        Assert.All(item.Attr, attr => Assert.NotNull(attr.Attributes));
        Assert.Equal("1", item.Season);
        Assert.Equal("1", item.Episode);
        Assert.Equal("500000000", item.Size);
        Assert.Equal("11", item.EpNumberFormatted);
    }

    [Fact]
    public async Task NZBPlanetSearch_PaginatesWhenFirstPageHasMoreThan99Items()
    {
        var firstPageItems = string.Join(",", Enumerable.Range(0, 100)
            .Select(i => $$"""{ "title": "Item {{i}}", "category": "TV" }"""));
        var firstPage = $$"""{ "channel": { "title": "x", "item": [{{firstPageItems}}] } }""";
        var secondPage = """{ "channel": { "title": "x", "item": [{ "title": "Extra Item", "category": "TV" }] } }""";

        using var httpTest = new HttpTest();
        httpTest.RespondWith(firstPage);
        httpTest.RespondWith(secondPage);

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var result = await service.NZBPlanetSearch(new Show { showid = 1 });

        Assert.Equal(101, result!.Channel.Item.Count);
        httpTest.ShouldHaveCalled("*offset=100*");
    }

    [Fact]
    public async Task NZBPlanetSearch_ReturnsNull_OnHttpFailure()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("error", 500);

        using var db = new TestDb();
        var service = TestFactory.CreateAppService(db);

        var result = await service.NZBPlanetSearch(new Show { showid = 1 });

        Assert.Null(result);
    }
}
