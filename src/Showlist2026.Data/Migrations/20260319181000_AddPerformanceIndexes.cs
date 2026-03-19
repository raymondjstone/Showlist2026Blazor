using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Episode: composite index on season, number, AirDateOffset2 with commonly selected columns
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Episode_Season_Number_AirDateOffset2' AND object_id = OBJECT_ID('Episode'))
                CREATE NONCLUSTERED INDEX [IX_Episode_Season_Number_AirDateOffset2]
                ON [dbo].[Episode] ([season], [number], [AirDateOffset2])
                INCLUDE ([episodeid], [name], [airdate], [airtime], [runtime], [summary], [links], [showId], [imagemedium], [imageoriginal]);
            ");

            // Show: index on needsupdate for background job queries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Show_NeedsUpdate' AND object_id = OBJECT_ID('Show'))
                CREATE NONCLUSTERED INDEX [IX_Show_NeedsUpdate]
                ON [dbo].[Show] ([needsupdate]);
            ");

            // Show: index on showid (TVMaze ID) for lookups
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Show_ShowId' AND object_id = OBJECT_ID('Show'))
                CREATE NONCLUSTERED INDEX [IX_Show_ShowId]
                ON [dbo].[Show] ([showid]);
            ");

            // Genre: index on genretextId with showId for genre-based filtering
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Genre_GenreTextIdPerf' AND object_id = OBJECT_ID('Genre'))
                CREATE NONCLUSTERED INDEX [IX_Genre_GenreTextIdPerf]
                ON [dbo].[Genre] ([genretextId])
                INCLUDE ([showId]);
            ");

            // UserShowSelection: index on include, Priority with showId for wanted/excluded queries
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserShowSelection_Include_Priority' AND object_id = OBJECT_ID('UserShowSelection'))
                CREATE NONCLUSTERED INDEX [IX_UserShowSelection_Include_Priority]
                ON [dbo].[UserShowSelection] ([include], [Priority])
                INCLUDE ([showId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Episode_Season_Number_AirDateOffset2] ON [dbo].[Episode];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Show_NeedsUpdate] ON [dbo].[Show];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Show_ShowId] ON [dbo].[Show];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Genre_GenreTextIdPerf] ON [dbo].[Genre];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_UserShowSelection_Include_Priority] ON [dbo].[UserShowSelection];");
        }
    }
}
