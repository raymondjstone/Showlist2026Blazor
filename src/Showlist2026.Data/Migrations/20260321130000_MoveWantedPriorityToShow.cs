using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveWantedPriorityToShow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Wanted column (nullable bit) to Show if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Show') AND name = 'Wanted')
                ALTER TABLE [dbo].[Show] ADD [Wanted] BIT NULL;
            ");

            // Add Priority column (int, default 0) to Show if not exists
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Show') AND name = 'Priority')
                ALTER TABLE [dbo].[Show] ADD [Priority] INT NOT NULL DEFAULT 0;
            ");

            // Populate Wanted from UserShowSelection if the table exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserShowSelection')
                BEGIN
                    -- Set wanted shows (include = 1)
                    UPDATE s
                    SET s.Wanted = 1, s.Priority = ISNULL(uss.Priority, 0)
                    FROM [dbo].[Show] s
                    INNER JOIN [dbo].[UserShowSelection] uss ON uss.showId = s.Id
                    WHERE uss.[include] = 1;

                    -- Set excluded shows (include = 0)
                    UPDATE s
                    SET s.Wanted = 0
                    FROM [dbo].[Show] s
                    INNER JOIN [dbo].[UserShowSelection] uss ON uss.showId = s.Id
                    WHERE uss.[include] = 0;
                END
            ");

            // Drop defunct junction table
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserShowSelection')
                DROP TABLE [dbo].[UserShowSelection];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Show') AND name = 'Wanted')
                ALTER TABLE [dbo].[Show] DROP COLUMN [Wanted];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Show') AND name = 'Priority')
                ALTER TABLE [dbo].[Show] DROP COLUMN [Priority];
            ");
        }
    }
}
