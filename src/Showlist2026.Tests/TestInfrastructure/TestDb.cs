using Microsoft.EntityFrameworkCore;
using Showlist2026.Data;

namespace Showlist2026.Tests.TestInfrastructure;

/// <summary>
/// Backs a test with a private, isolated EF Core InMemory database (a fresh named database
/// per <see cref="TestDb"/> instance, so tests never see each other's data).
///
/// Why InMemory and not Sqlite: production runs on SQL Server. Of the two fake providers
/// available for testing, Sqlite is relational but cannot translate relational operators
/// (&lt;, &gt;) on <c>DateTimeOffset</c> columns at all (a documented EF Core Sqlite limitation —
/// it only supports equality, to avoid silently-wrong results from lexicographic string
/// comparison across differing offsets). Since nearly every query in this app filters
/// episodes by AirDateOffset2 range, Sqlite can't run them. InMemory evaluates predicates
/// client-side, so DateTimeOffset comparisons just work.
///
/// The trade-off: InMemory does not support raw SQL (<c>FromSqlRaw</c>), which
/// <c>UndecidedShows()</c> uses for an efficient "latest episode per show" lookup. That one
/// code path isn't exercised end-to-end by this suite (see the tests for that method) —
/// production's real SQL Server backend supports both raw SQL and DateTimeOffset ranges fine.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly DbContextOptions<ShowlistDbContext> _options;

    public DbContextOptions<ShowlistDbContext> Options => _options;

    public TestDb()
    {
        _options = new DbContextOptionsBuilder<ShowlistDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public ShowlistDbContext CreateContext() => new(_options);

    public void Dispose() { }
}

/// <summary>Hands out contexts against a single <see cref="TestDb"/>'s in-memory database.</summary>
public sealed class TestDbContextFactory : IDbContextFactory<ShowlistDbContext>
{
    private readonly DbContextOptions<ShowlistDbContext> _options;
    public TestDbContextFactory(DbContextOptions<ShowlistDbContext> options) => _options = options;
    public ShowlistDbContext CreateDbContext() => new(_options);
}
