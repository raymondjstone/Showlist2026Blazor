using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropAbpTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop all foreign keys on ABP tables first, then drop the tables
            migrationBuilder.Sql(@"
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql += 'ALTER TABLE [' + OBJECT_SCHEMA_NAME(parent_object_id) + '].[' + OBJECT_NAME(parent_object_id) + '] DROP CONSTRAINT [' + name + '];' + CHAR(13)
                FROM sys.foreign_keys
                WHERE OBJECT_NAME(parent_object_id) LIKE 'Abp%' OR OBJECT_NAME(referenced_object_id) LIKE 'Abp%';
                EXEC sp_executesql @sql;
            ");

            migrationBuilder.Sql(@"
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql += 'DROP TABLE [' + SCHEMA_NAME(schema_id) + '].[' + name + '];' + CHAR(13)
                FROM sys.tables
                WHERE name LIKE 'Abp%';
                EXEC sp_executesql @sql;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_GenreText_genretextId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_Show_showId",
                table: "Genre");


            // Drop schemabinding views BEFORE column changes (they block ALTER COLUMN)
            migrationBuilder.Sql("IF OBJECT_ID('IsWanted_Indexed', 'V') IS NOT NULL DROP VIEW [dbo].[IsWanted_Indexed];");
            migrationBuilder.Sql("IF OBJECT_ID('IsWanted', 'V') IS NOT NULL DROP VIEW [dbo].[IsWanted];");

            // Drop ALL objects depending on UserId columns before dropping them
            migrationBuilder.Sql(@"
                DECLARE @sql NVARCHAR(MAX) = '';

                -- Drop check constraints referencing UserId
                SELECT @sql += 'ALTER TABLE [' + SCHEMA_NAME(t.schema_id) + '].[' + t.name + '] DROP CONSTRAINT [' + cc.name + '];' + CHAR(13)
                FROM sys.check_constraints cc
                INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
                WHERE cc.definition LIKE '%UserId%' AND t.name LIKE 'User%Selection';

                -- Drop default constraints on UserId
                SELECT @sql += 'ALTER TABLE [' + SCHEMA_NAME(t.schema_id) + '].[' + t.name + '] DROP CONSTRAINT [' + dc.name + '];' + CHAR(13)
                FROM sys.default_constraints dc
                INNER JOIN sys.tables t ON dc.parent_object_id = t.object_id
                INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                WHERE c.name = 'UserId' AND t.name LIKE 'User%Selection';

                -- Drop computed columns that reference UserId
                SELECT @sql += 'ALTER TABLE [' + SCHEMA_NAME(t.schema_id) + '].[' + t.name + '] DROP COLUMN [' + c.name + '];' + CHAR(13)
                FROM sys.computed_columns c
                INNER JOIN sys.tables t ON c.object_id = t.object_id
                WHERE c.definition LIKE '%UserId%' AND t.name LIKE 'User%Selection';

                -- Drop statistics referencing UserId
                SELECT @sql += 'DROP STATISTICS [' + SCHEMA_NAME(t.schema_id) + '].[' + t.name + '].[' + s.name + '];' + CHAR(13)
                FROM sys.stats s
                INNER JOIN sys.tables t ON s.object_id = t.object_id
                INNER JOIN sys.stats_columns sc ON s.object_id = sc.object_id AND s.stats_id = sc.stats_id
                INNER JOIN sys.columns c ON sc.object_id = c.object_id AND sc.column_id = c.column_id
                WHERE c.name = 'UserId' AND t.name LIKE 'User%Selection'
                AND s.name NOT LIKE 'PK_%' AND s.auto_created = 0 AND s.user_created = 1;

                -- Drop all indexes (including filtered) referencing UserId
                SELECT @sql += 'DROP INDEX [' + i.name + '] ON [' + SCHEMA_NAME(t.schema_id) + '].[' + t.name + '];' + CHAR(13)
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                WHERE t.name LIKE 'User%Selection' AND i.is_primary_key = 0 AND i.type > 0
                AND (
                    EXISTS (
                        SELECT 1 FROM sys.index_columns ic
                        INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                        WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND c.name = 'UserId'
                    )
                    OR i.filter_definition LIKE '%UserId%'
                );

                EXEC sp_executesql @sql;
            ");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserWebNetworkSelection");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserWatchedSelection");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserTypeSelection");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserShowSelection");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserNetworkSelection");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserLanguageSelection");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserGenreSelection");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserCountrySelection");

            migrationBuilder.AlterColumn<int>(
                name: "showId",
                table: "Genre",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "genretextId",
                table: "Genre",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<long>(
                name: "season",
                table: "Episode",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "number",
                table: "Episode",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_GenreText_genretextId",
                table: "Genre",
                column: "genretextId",
                principalTable: "GenreText",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_Show_showId",
                table: "Genre",
                column: "showId",
                principalTable: "Show",
                principalColumn: "Id");

            // Recreate views without UserId after column changes are done
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
                WHERE
                    EXISTS (
                        SELECT 1
                        FROM dbo.UserShowSelection uss
                        WHERE uss.showId = s.Id
                            AND uss.include = 1
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM dbo.UserWatchedSelection uws
                        WHERE uws.episodeId = e.Id
                    )
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
                WHERE
                    EXISTS (
                        SELECT 1
                        FROM dbo.UserShowSelection uss
                        WHERE uss.showId = s.Id
                            AND uss.include = 1
                    )
                    AND NOT EXISTS (
                        SELECT 1
                        FROM dbo.UserWatchedSelection uws
                        WHERE uws.episodeId = e.Id
                    )
                    AND e.AirDateOffset2 < GETDATE();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Genre_GenreText_genretextId",
                table: "Genre");

            migrationBuilder.DropForeignKey(
                name: "FK_Genre_Show_showId",
                table: "Genre");

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserWebNetworkSelection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserWatchedSelection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserTypeSelection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserShowSelection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserNetworkSelection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserLanguageSelection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserGenreSelection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "UserCountrySelection",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<int>(
                name: "showId",
                table: "Genre",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "genretextId",
                table: "Genre",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "season",
                table: "Episode",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "number",
                table: "Episode",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_GenreText_genretextId",
                table: "Genre",
                column: "genretextId",
                principalTable: "GenreText",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Genre_Show_showId",
                table: "Genre",
                column: "showId",
                principalTable: "Show",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
