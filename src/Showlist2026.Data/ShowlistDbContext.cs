using Microsoft.EntityFrameworkCore;
using Showlist2026.Entities;
using Type = Showlist2026.Entities.Type;

namespace Showlist2026.Data
{
    public class ShowlistDbContext : DbContext
    {
        public DbSet<Timezone> Timezones { get; set; }
        public DbSet<Country> Countrys { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<GenreText> GenreTexts { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Network> Networks { get; set; }
        public DbSet<Show> Shows { get; set; }
        public DbSet<ShowUpdated> ShowUpdateds { get; set; }
        public DbSet<Type> Types { get; set; }
        public DbSet<WebNetwork> WebNetworks { get; set; }
        public DbSet<TVDirectories> TVDirectories { get; set; }
        public DbSet<TVSite> TVSites { get; set; }
        public DbSet<TouchFile> TouchFiles { get; set; }
        public DbSet<TouchFolder> TouchFolder { get; set; }
        public DbSet<WatchedHistory> WatchedHistories { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        public ShowlistDbContext(DbContextOptions<ShowlistDbContext> options)
            : base(options)
        {
            if (Database.IsRelational())
            {
                Database.SetCommandTimeout(60);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
