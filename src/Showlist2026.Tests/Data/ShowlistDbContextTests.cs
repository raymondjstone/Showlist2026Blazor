using Microsoft.EntityFrameworkCore;
using Showlist2026.Data;
using Xunit;

namespace Showlist2026.Tests.Data;

public class ShowlistDbContextTests
{
    [Fact]
    public void Constructor_SetsCommandTimeout_ForRelationalProviders()
    {
        // Only the relational branch (Database.IsRelational()) is exercised by the rest of the
        // suite (which uses the InMemory provider, where IsRelational() is false). Constructing
        // against a relational provider here closes that gap - SqlServer options never actually
        // open a connection until a query runs, so no real SQL Server is needed.
        var options = new DbContextOptionsBuilder<ShowlistDbContext>()
            .UseSqlServer("Server=(local);Database=ShowlistTestsNeverConnected;Trusted_Connection=True;")
            .Options;

        using var db = new ShowlistDbContext(options);

        Assert.True(db.Database.IsRelational());
        Assert.Equal(60, db.Database.GetCommandTimeout());
    }

    [Fact]
    public void Constructor_DoesNotSetCommandTimeout_ForNonRelationalProviders()
    {
        var options = new DbContextOptionsBuilder<ShowlistDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new ShowlistDbContext(options);

        Assert.False(db.Database.IsRelational());
    }

    [Fact]
    public void OnModelCreating_ConfiguresShowLinkForeignKeysWithoutCascadeDelete()
    {
        // ShowLink has two FKs to Show; without DeleteBehavior.Restrict on both, EF would throw
        // a multi-cascade-path model-building error. If the model builds at all, this held.
        var options = new DbContextOptionsBuilder<ShowlistDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new ShowlistDbContext(options);
        var entityType = db.Model.FindEntityType(typeof(Showlist2026.Entities.ShowLink));

        Assert.NotNull(entityType);
        var foreignKeys = entityType!.GetForeignKeys().ToList();
        Assert.Equal(2, foreignKeys.Count);
        Assert.All(foreignKeys, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }
}
