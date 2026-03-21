using Microsoft.EntityFrameworkCore;
using Showlist2026.Data;

namespace Showlist2026.Web.Configuration
{
    public class DbConfigurationProvider : ConfigurationProvider
    {
        private readonly string _connectionString;

        public DbConfigurationProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override void Load()
        {
            try
            {
                var options = new DbContextOptionsBuilder<ShowlistDbContext>()
                    .UseSqlServer(_connectionString)
                    .Options;

                using var db = new ShowlistDbContext(options);

                // Check if the table exists before querying
                if (db.Database.CanConnect())
                {
                    try
                    {
                        var settings = db.AppSettings.ToList();
                        Data = settings.ToDictionary(s => s.Key, s => s.Value,
                            StringComparer.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        // Table may not exist yet (migration not applied)
                        Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                // DB not available at startup — skip
                Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public class DbConfigurationSource : IConfigurationSource
    {
        private readonly string _connectionString;

        public DbConfigurationSource(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DbConfigurationProvider(_connectionString);
        }
    }
}
