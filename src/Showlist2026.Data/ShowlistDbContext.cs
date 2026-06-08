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
        public DbSet<Type> Types { get; set; }
        public DbSet<WebNetwork> WebNetworks { get; set; }
        public DbSet<TVDirectories> TVDirectories { get; set; }
        public DbSet<TVSite> TVSites { get; set; }
        public DbSet<TouchFile> TouchFiles { get; set; }
        public DbSet<TouchFolder> TouchFolder { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<ShowFolderAlias> ShowFolderAliases { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<FriendShow> FriendShows { get; set; }
        public DbSet<FriendCopy> FriendCopies { get; set; }
        public DbSet<ShowLink> ShowLinks { get; set; }

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

            // ShowLink has two FKs to Show — disable cascade delete to avoid multi-cascade-path error
            modelBuilder.Entity<ShowLink>()
                .HasOne(sl => sl.PredecessorShow)
                .WithMany()
                .HasForeignKey(sl => sl.PredecessorShowId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShowLink>()
                .HasOne(sl => sl.SuccessorShow)
                .WithMany()
                .HasForeignKey(sl => sl.SuccessorShowId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
