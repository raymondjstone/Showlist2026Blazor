using Showlist2026.Entities;
using Type = Showlist2026.Entities.Type;

namespace Showlist2026.Tests.TestInfrastructure;

/// <summary>Small builders for entity graphs so service tests stay focused on behaviour, not setup.</summary>
public static class TestData
{
    public static Show NewShow(
        string name,
        long showid = 0,
        bool? wanted = null,
        string? premiered = null,
        string? folderName = null,
        Network? network = null,
        WebNetwork? webNetwork = null,
        Type? type = null,
        Language? language = null,
        string? status = null,
        int priority = 0)
    {
        return new Show
        {
            showid = showid,
            name = name,
            Wanted = wanted,
            premiered = premiered,
            FolderName = folderName,
            Networks = network,
            WebNetworks = webNetwork,
            Types = type,
            Languages = language,
            status = status,
            Priority = priority,
            page = 0,
        };
    }

    public static Episode NewEpisode(
        Show show,
        long season,
        long number,
        DateTimeOffset? airDate = null,
        bool watched = false,
        bool givenUp = false,
        long episodeid = 0,
        string? name = null,
        string? runtime = null)
    {
        var ep = new Episode
        {
            episodeid = episodeid,
            show = show,
            season = season,
            number = number,
            AirDateOffset2 = airDate,
            Watched = watched,
            GivenUp = givenUp,
            name = name,
            runtime = runtime,
        };
        show.Episodes ??= new List<Episode>();
        show.Episodes.Add(ep);
        return ep;
    }

    public static Network NewNetwork(string name, Country? country = null, bool? wanted = null) => new()
    {
        networkid = new Random().NextInt64(1, int.MaxValue),
        name = name,
        country = country,
        Wanted = wanted,
    };

    public static Country NewCountry(string code, string? name = null) => new() { code = code, name = name ?? code };

    public static GenreText NewGenreText(string genre, bool? wanted = null) => new() { genre = genre, Wanted = wanted };

    public static Language NewLanguage(string name, bool? wanted = null) => new() { name = name, Wanted = wanted };

    public static Type NewType(string type, bool? wanted = null) => new() { type = type, Wanted = wanted };

    public static WebNetwork NewWebNetwork(string name, Country? country = null, bool? wanted = null) => new()
    {
        webid = new Random().NextInt64(1, int.MaxValue),
        name = name,
        country = country,
        Wanted = wanted,
    };
}
