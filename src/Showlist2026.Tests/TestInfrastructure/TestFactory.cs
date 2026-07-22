using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Showlist2026.Configuration;
using Showlist2026.Services;

namespace Showlist2026.Tests.TestInfrastructure;

public static class TestFactory
{
    public static ShowlistOptions Options(string? tvNameListPath = null) => new()
    {
        TvNameListPath = tvNameListPath ?? "",
        ShowFolderBasePath = "",
        TvMazeBaseUrl = "http://api.tvmaze.invalid",
        NzbPlanetApiKey = ""
    };

    public static ShowListAppService CreateAppService(
        TestDb db,
        ShowlistOptions? options = null,
        FakeNotificationService? notifications = null)
    {
        return new ShowListAppService(
            new TestDbContextFactory(db.Options),
            NullLogger<ShowListAppService>.Instance,
            Microsoft.Extensions.Options.Options.Create(options ?? Options()),
            notifications ?? new FakeNotificationService());
    }

    public static ShowListBackgroundService CreateBackgroundService(
        Showlist2026.Data.ShowlistDbContext context,
        ShowlistOptions? options = null,
        FakeNotificationService? notifications = null)
    {
        return new ShowListBackgroundService(
            context,
            NullLogger<ShowListBackgroundService>.Instance,
            Microsoft.Extensions.Options.Options.Create(options ?? Options()),
            notifications ?? new FakeNotificationService());
    }
}
