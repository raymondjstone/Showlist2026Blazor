using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Showlist2026.Configuration;
using Showlist2026.Data;
using Showlist2026.Services;
using Showlist2026.Web.Components;
using Showlist2026.Web.Configuration;
using Showlist2026.Web.Health;
using Showlist2026.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("SHOWLIST_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("Default")!;

// Add DB settings as highest-priority config source (overrides appsettings, env vars, user secrets)
((IConfigurationBuilder)builder.Configuration).Add(new DbConfigurationSource(connectionString));

// Configuration
builder.Services.Configure<ShowlistOptions>(builder.Configuration.GetSection("Showlist"));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notifications"));

// EF Core
builder.Services.AddDbContextFactory<ShowlistDbContext>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Services
builder.Services.AddScoped<IShowListAppService, ShowListAppService>();
builder.Services.AddScoped<IShowListBackgroundService, ShowListBackgroundService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IJobStatusService, JobStatusService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "database")
    .AddCheck<TvMazeHealthCheck>("tvmaze-api");

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer();

// MVC Controllers (for RSS endpoints)
builder.Services.AddControllers();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-apply EF Core migrations on startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ShowlistDbContext>();
    db.Database.Migrate();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database migration failed on startup");
    throw;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// Health check endpoint
app.MapHealthChecks("/health");

// Hangfire Dashboard (allow remote access - single user app)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
});

// Register recurring jobs
RecurringJob.AddOrUpdate<IShowListBackgroundService>(
    "PopulateShowFolderNames", s => s.PopulateShowFolderNames(), "25 0,3,6,9,12,15,18,21 * * *");
RecurringJob.AddOrUpdate<IShowListBackgroundService>(
    "ShowDownloadedJob", s => s.ShowDownloadedJob(), "0,10,20,30,40,50 * * * *");
RecurringJob.AddOrUpdate<IShowListBackgroundService>(
    "RefreshShows", s => s.RefreshShows(), "22,42,02 * * * *");
RecurringJob.AddOrUpdate<IShowListBackgroundService>(
    "RefreshShowDates", s => s.RefreshShowDates(), "06 * * * *");
RecurringJob.AddOrUpdate<IShowListAppService>(
    "NewSeasonNotifications", s => s.CheckNewSeasonNotifications(), "0 8 * * *");
    RecurringJob.AddOrUpdate<IShowListBackgroundService>(
    "ResolveAliasFolders", s => s.ResolveAliasFolders(), "5,15,25,35,45,55 * * * *");

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Allow all access to Hangfire dashboard (single-user app, no auth needed)
public class AllowAllDashboardAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
