# Test suite notes

## Two separate coverage engines — both need excluding Migrations, independently

There are two unrelated ways to see a coverage number for this solution, each with its own
config file, because neither reads the other's settings:

1. **`dotnet test` (coverlet)** — collected automatically on every plain `dotnet test`, no
   flags needed. Config lives in the `PropertyGroup` in `Showlist2026.Tests.csproj`
   (`CollectCoverage`, `Exclude`, etc.).
2. **Visual Studio's "Analyze Code Coverage for All Tests"** — a completely different engine
   (Microsoft's own `datacollector://Microsoft/CodeCoverage/2.0`), configured by
   **`CodeCoverage.runsettings` at the repo root**. VS 2019 16.4+ auto-detects a single
   `.runsettings` file at the solution root, so this applies without going through
   Test → Configure Run Settings. It also reports a different default metric (**block**
   coverage, not line coverage) — if a number you see doesn't match what's quoted here, check
   which of the two you're looking at, and which metric (VS shows block % by default; the
   figures below are line %).

Current numbers (line %, coverlet):

| Project | Line | Branch |
|---|---|---|
| Core | ~89% | ~77% |
| Data | 100% | 100% |
| Web  | ~46% | ~34% |

Only one exclusion remains in either config: `Migrations` (EF Core scaffolded DDL — see
below). Everything else that's untested shows up as untested in both tools; nothing is hidden.

## Remaining gaps, and why

**EF Core migrations (Data) — needs a real SQL Server.**
These are generated T-SQL/DDL scripts, only meaningfully exercised by `Database.Migrate()`
against an actual SQL Server instance. Testcontainers (`Testcontainers.MsSql`) would let this
run against a real, disposable SQL Server container per test run — Docker is installed on this
machine but the daemon wasn't running when this was set up. Ask if you'd like this wired up;
it needs Docker Desktop started first.

**Dead code (Core) — deleted, not tested.** (Done — this section is now historical.)
The following were confirmed unreferenced anywhere in the app (including by
`ShowlistDbContext` — old migration files reference some of them, but only as string table
names, e.g. `name: "UserShowSelection"`, never as an actual C# type; a later `DropAbpTables`
migration already dropped these tables from the real schema, and the current
`ShowlistDbContextModelSnapshot.cs` has no trace of them) and removed outright rather than
tested, since testing unreachable code verifies nothing. This alone took Core from ~75% to
~89% line coverage:
- Entities: `WatchedHistory`, `Scheduled`, `ShowUpdated`, `SSENData`, `UserShowSelection`,
  `UserCountrySelection`, `UserGenreSelection`, `UserGivenUpSelection`, `UserLanguageSelection`,
  `UserNetworkSelection`, `UserTypeSelection`, `UserWebNetworkSelection`, `UserWatchedSelection`
  — leftovers from when these were separate join tables, superseded by a `Wanted` property
  directly on each filterable entity.
- `Showlist2026.TVMaze.TVMazeShowUpdated` — defined, never used anywhere.
- From `NZBPlanetApiJSON.cs`: `NameConverter`, `ParseStringConverter`, `Converter`, `NzBplanet`,
  `Serialize` (Newtonsoft-only deserialization helpers `ShowListAppService.NZBPlanetSearch`
  never calls — it deserializes via Flurl/System.Text.Json instead, see the fixed bug in
  `NZBPlanetSearchTests.cs`), plus `Response`/`ResponseAttributes` and `TypeEnum` (also
  unreferenced; removed at the same time since `ResponseAttributes` depended on the also-dead
  `ParseStringConverter`). `NzBplanetJSON`, `Channel`, `Item`, `Attr`, `Enclosure`, and their
  live fields all stay — that's the real, tested API contract.

**Blazor UI (Web) — in progress, not stalled.**
`Showlist2026.Tests/Components/` now has real bUnit tests (`FilterButtons`, `EpisodeRow`,
`FiltersNetwork`/`Country`/`Genre`/`Language`/`Type`/`WebNetwork`/`Show`, `MissedStuff`,
`GivenUp`, `Home`, `NoFolder`, `Undecided`) — each renders the REAL page against the REAL
`ShowListAppService`/`ShowListBackgroundService`/`IJobStatusService` backed by an isolated
in-memory database (`BlazorTestBase`), not mocks. Clicking buttons in these tests goes through
actual service calls and actually persists to the (test) database. The pattern is proven and
cheap to repeat — extending it to the remaining pages (`ShowDetail`, `Admin*`, `Friends`,
`Compare`, `Storage`, `Dedupe`, `Calendar`, `AdvancedSearch`, `Statistics`, `Trending`,
`ComingSoon`, `DownloadedShow`, `DownloadProgress`, `FiltersNZBSites`, `NextUnwatched`,
`AiringAroundNow`, and the smaller shared components) is more of the same work, not a new
investment.

**HTTP-dependent code that bypasses Flurl — can't be intercepted by `HttpTest`.**
`CrawlNzbSitesForShow`/`CrawlNzbRssFeedsForShow` and their private `CrawlWith*` helpers
construct `new HttpClient()` directly instead of using Flurl, so `Flurl.Http.Testing.HttpTest`
(used everywhere else in this suite to mock TVMaze/Discord calls) can't intercept them. Testing
these would require refactoring them to accept an injected `HttpClient`/`IHttpClientFactory` —
a production code change, not just a test addition. Not done without being asked.

**`RefreshNetworks` — hardcoded to loop at least 1800 times.**
`maxnet` starts at 1800 and can only grow (never shrink) based on existing data, so even a
mocked-HTTP unit test would need ~1800 awaited round trips. Technically possible, but slow and
not a good unit test. Left uncovered.

**`NotificationService.SendPushoverAsync`/`SendEmailAsync`.**
Pushover uses `Altairis.Pushover.Client` (not Flurl) and Email uses a raw `SmtpClient` — neither
is interceptable the way Discord's Flurl-based webhook call is (which *is* tested). Same
"needs a production seam" situation as the NZB crawlers.
