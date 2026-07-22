using Showlist2026.Entities;
using Showlist2026.Tests.TestInfrastructure;
using Xunit;

namespace Showlist2026.Tests.Data;

/// <summary>
/// AppSetting itself is a plain record with no logic - this verifies it round-trips through
/// the real DbContext (the thing that actually matters: DbConfigurationProvider reads this
/// table at startup), rather than just re-asserting auto-property getters/setters.
/// </summary>
public class AppSettingTests
{
    [Fact]
    public void RoundTripsThroughTheDbContext()
    {
        using var db = new TestDb();
        using (var ctx = db.CreateContext())
        {
            ctx.AppSettings.Add(new AppSetting { Key = "MySetting", Value = "42" });
            ctx.SaveChanges();
        }

        using var verify = db.CreateContext();
        var setting = verify.AppSettings.Find("MySetting");

        Assert.NotNull(setting);
        Assert.Equal("42", setting!.Value);
    }
}
