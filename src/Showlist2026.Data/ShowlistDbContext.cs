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
        public DbSet<UserGenreSelection> UserGenreSelections { get; set; }
        public DbSet<UserCountrySelection> UserCountrySelections { get; set; }
        public DbSet<UserLanguageSelection> UserLanguageSelections { get; set; }
        public DbSet<UserNetworkSelection> UserNetworkSelections { get; set; }
        public DbSet<UserWebNetworkSelection> UserWebNetworkSelections { get; set; }
        public DbSet<UserShowSelection> UserShowSelections { get; set; }
        public DbSet<UserTypeSelection> UserTypeSelections { get; set; }
        public DbSet<UserWatchedSelection> UserWatchedSelections { get; set; }
        public DbSet<WebNetwork> WebNetworks { get; set; }
        public DbSet<TVDirectories> TVDirectories { get; set; }
        public DbSet<TVSite> TVSites { get; set; }
        public DbSet<TouchFile> TouchFiles { get; set; }
        public DbSet<TouchFolder> TouchFolder { get; set; }
        public DbSet<WatchedHistory> WatchedHistories { get; set; }

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

            // UserId column still exists in DB but is ignored by EF (always 1, single-user app)
            modelBuilder.Entity<UserShowSelection>().Ignore("UserId");
            modelBuilder.Entity<UserGenreSelection>().Ignore("UserId");
            modelBuilder.Entity<UserLanguageSelection>().Ignore("UserId");
            modelBuilder.Entity<UserNetworkSelection>().Ignore("UserId");
            modelBuilder.Entity<UserWebNetworkSelection>().Ignore("UserId");
            modelBuilder.Entity<UserCountrySelection>().Ignore("UserId");
            modelBuilder.Entity<UserTypeSelection>().Ignore("UserId");
            modelBuilder.Entity<UserWatchedSelection>().Ignore("UserId");
        }
    }
}
