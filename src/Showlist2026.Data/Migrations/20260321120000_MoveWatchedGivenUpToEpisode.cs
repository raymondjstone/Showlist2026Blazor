using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveWatchedGivenUpToEpisode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop schemabinding view first (blocks table drops), then other views
            migrationBuilder.Sql("IF OBJECT_ID('IsWanted_Indexed', 'V') IS NOT NULL DROP VIEW [dbo].[IsWanted_Indexed];");
            migrationBuilder.Sql("IF OBJECT_ID('IsWanted', 'V') IS NOT NULL DROP VIEW [dbo].[IsWanted];");
            migrationBuilder.Sql("IF OBJECT_ID('newshows_withfolder', 'V') IS NOT NULL DROP VIEW [dbo].[newshows_withfolder];");
            migrationBuilder.Sql("IF OBJECT_ID('shows_with_missing_folders', 'V') IS NOT NULL DROP VIEW [dbo].[shows_with_missing_folders];");

            // Add Watched column to Episode if not exists, default false
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Episode') AND name = 'Watched')
                ALTER TABLE [dbo].[Episode] ADD [Watched] BIT NOT NULL DEFAULT 0;
            ");

            // Add GivenUp column to Episode if not exists, default false
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Episode') AND name = 'GivenUp')
                ALTER TABLE [dbo].[Episode] ADD [GivenUp] BIT NOT NULL DEFAULT 0;
            ");

            // Populate Watched from UserWatchedSelection if the table exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserWatchedSelection')
                BEGIN
                    UPDATE e
                    SET e.Watched = 1
                    FROM [dbo].[Episode] e
                    INNER JOIN [dbo].[UserWatchedSelection] uws ON uws.episodeId = e.Id;
                END
            ");

            // Populate GivenUp from UserGivenUpSelection if the table exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserGivenUpSelection')
                BEGIN
                    UPDATE e
                    SET e.GivenUp = 1
                    FROM [dbo].[Episode] e
                    INNER JOIN [dbo].[UserGivenUpSelection] ugs ON ugs.episodeId = e.Id;
                END
            ");

            // Drop defunct junction tables
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserWatchedSelection')
                DROP TABLE [dbo].[UserWatchedSelection];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserGivenUpSelection')
                DROP TABLE [dbo].[UserGivenUpSelection];
            ");

            // Recreate views using new columns instead of junction tables
            migrationBuilder.Sql(@"
                CREATE VIEW [dbo].[IsWanted] AS
                SELECT
                    'http://showlist/showlist/show/' + CONVERT(VARCHAR(7), s.Id) AS show,
                    s.name,
                    e.name AS epName,
                    e.season,
                    e.number,
                    e.episodeid,
                    s.Id AS showid,
                    e.Id,
                    e.AirDateOffset2
                FROM dbo.Show s
                INNER JOIN dbo.Episode e ON e.showId = s.Id
                WHERE s.Wanted = 1
                    AND e.Watched = 0
                    AND e.AirDateOffset2 < GETDATE();
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW [dbo].[IsWanted_Indexed]
                WITH SCHEMABINDING AS
                SELECT
                    'http://showlist/showlist/show/' + CONVERT(VARCHAR(7), s.Id) AS show,
                    s.name,
                    e.name AS epName,
                    e.season,
                    e.number,
                    e.episodeid,
                    s.Id AS showid,
                    e.Id,
                    e.AirDateOffset2
                FROM dbo.Show s
                INNER JOIN dbo.Episode e ON e.showId = s.Id
                WHERE s.Wanted = 1
                    AND e.Watched = 0
                    AND e.AirDateOffset2 < GETDATE();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Episode') AND name = 'Watched')
                ALTER TABLE [dbo].[Episode] DROP COLUMN [Watched];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Episode') AND name = 'GivenUp')
                ALTER TABLE [dbo].[Episode] DROP COLUMN [GivenUp];
            ");
        }
    }
}
