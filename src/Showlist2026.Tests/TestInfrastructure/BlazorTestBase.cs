using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Showlist2026.Services;
using Showlist2026.Web.Services;

namespace Showlist2026.Tests.TestInfrastructure;

/// <summary>
/// Base class for bUnit component tests. Registers the REAL ShowListAppService/
/// ShowListBackgroundService (backed by an isolated InMemory TestDb) into the component's DI
/// container, so rendering a page exercises actual data-loading and mutation logic end to end -
/// not mocks standing in for it.
/// </summary>
public abstract class BlazorTestBase : BunitContext
{
    protected TestDb Db { get; }
    protected FakeNotificationService Notifications { get; } = new();

    protected BlazorTestBase()
    {
        Db = new TestDb();
        Services.AddSingleton<IShowListAppService>(TestFactory.CreateAppService(Db, notifications: Notifications));
        Services.AddSingleton<IShowListBackgroundService>(_ =>
            TestFactory.CreateBackgroundService(Db.CreateContext(), notifications: Notifications));
        Services.AddSingleton<IJobStatusService, JobStatusService>();
        Services.AddSingleton<INotificationService>(Notifications);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Db.Dispose();
        base.Dispose(disposing);
    }
}
