using Microsoft.AspNetCore.Mvc;
using Showlist2026.Data;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;

namespace Showlist2026.Web.Controllers;

[Route("rss/watched")]
public class WatchedRssController : Controller
{
    private readonly ShowlistDbContext _db;

    public WatchedRssController(ShowlistDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var episodes = await _db.Episodes
            .Include(e => e.show)
            .Where(e => e.Watched && e.AirDateOffset2 != null)
            .OrderByDescending(e => e.AirDateOffset2)
            .Take(100)
            .ToListAsync();

        var items = episodes.Select(ep =>
        {
            var torrentName = BuildTorrentName(ep.show?.name, ep.EpNumberFormatted);
            var torrentUrl = new Uri($"{baseUrl}/rss/watched/torrent/{ep.episodeid}/{torrentName}.torrent");
            var item = new SyndicationItem(
                $"{ep.show?.name} - {ep.EpNumberFormatted} - {ep.name}",
                ep.summary ?? "",
                torrentUrl,
                ep.episodeid.ToString(),
                ep.AirDateOffset2 ?? DateTimeOffset.UtcNow
            );
            item.Links.Add(SyndicationLink.CreateMediaEnclosureLink(
                torrentUrl,
                "application/x-bittorrent",
                0
            ));
            return item;
        }).ToList();

        var feed = new SyndicationFeed(
            "Latest Watched Episodes",
            "The 200 most recently aired watched episodes.",
            new Uri($"{baseUrl}/rss/watched"),
            items
        );

        var ms = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false)
        });
        feed.SaveAsRss20(xmlWriter);
        xmlWriter.Flush();
        ms.Position = 0;
        return File(ms, "application/rss+xml");
    }

    [HttpGet("torrent/{id}/{filename}")]
    public IActionResult Torrent(long id, string filename)
    {
        // Build a minimal valid .torrent file (bencoded) with the filename as the torrent name
        var name = Path.GetFileNameWithoutExtension(filename);
        var torrentBytes = BuildFakeTorrent(name);
        return File(torrentBytes, "application/x-bittorrent", $"{name}.torrent");
    }

    private static string BuildTorrentName(string showName, string epNumber)
    {
        // "My Show!" -> "My.Show" , then append ".S01E01"
        var clean = showName ?? "Unknown";
        var sb = new StringBuilder();
        foreach (var c in clean)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c == ' ' || c == '.')
                sb.Append('.');
            // skip other special chars
        }
        // collapse consecutive dots
        var name = sb.ToString();
        while (name.Contains(".."))
            name = name.Replace("..", ".");
        name = name.Trim('.');

        return $"{name}.{epNumber}";
    }

    private static byte[] BuildFakeTorrent(string name)
    {
        // Minimal valid bencoded torrent:
        // d8:announce0:4:infod6:lengthi0e4:name<len>:<name>12:piece lengthi16384e6:pieces0:ee
        var sb = new StringBuilder();
        sb.Append("d8:announce0:");
        sb.Append("4:infod");
        sb.Append($"6:lengthi0e");
        sb.Append($"4:name{name.Length}:{name}");
        sb.Append($"12:piece lengthi16384e");
        sb.Append("6:pieces0:");
        sb.Append("ee");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
