using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveWantedToFilterEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Wanted column to Country
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Country') AND name = 'Wanted')
                ALTER TABLE [dbo].[Country] ADD [Wanted] BIT NULL;
            ");

            // Add Wanted column to GenreText
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('GenreText') AND name = 'Wanted')
                ALTER TABLE [dbo].[GenreText] ADD [Wanted] BIT NULL;
            ");

            // Add Wanted column to Language
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Language') AND name = 'Wanted')
                ALTER TABLE [dbo].[Language] ADD [Wanted] BIT NULL;
            ");

            // Add Wanted column to Network
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Network') AND name = 'Wanted')
                ALTER TABLE [dbo].[Network] ADD [Wanted] BIT NULL;
            ");

            // Add Wanted column to WebNetwork
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WebNetwork') AND name = 'Wanted')
                ALTER TABLE [dbo].[WebNetwork] ADD [Wanted] BIT NULL;
            ");

            // Add Wanted column to Type
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Type') AND name = 'Wanted')
                ALTER TABLE [dbo].[Type] ADD [Wanted] BIT NULL;
            ");

            // Populate Country.Wanted from UserCountrySelection
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserCountrySelection')
                BEGIN
                    UPDATE c
                    SET c.Wanted = CAST(ucs.[include] AS BIT)
                    FROM [dbo].[Country] c
                    INNER JOIN [dbo].[UserCountrySelection] ucs ON ucs.countryId = c.Id;
                END
            ");

            // Populate GenreText.Wanted from UserGenreSelection
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserGenreSelection')
                BEGIN
                    UPDATE gt
                    SET gt.Wanted = CAST(ugs.[include] AS BIT)
                    FROM [dbo].[GenreText] gt
                    INNER JOIN [dbo].[UserGenreSelection] ugs ON ugs.genretextId = gt.Id;
                END
            ");

            // Populate Language.Wanted from UserLanguageSelection
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserLanguageSelection')
                BEGIN
                    UPDATE l
                    SET l.Wanted = CAST(uls.[include] AS BIT)
                    FROM [dbo].[Language] l
                    INNER JOIN [dbo].[UserLanguageSelection] uls ON uls.languageId = l.Id;
                END
            ");

            // Populate Network.Wanted from UserNetworkSelection
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserNetworkSelection')
                BEGIN
                    UPDATE n
                    SET n.Wanted = CAST(uns.[include] AS BIT)
                    FROM [dbo].[Network] n
                    INNER JOIN [dbo].[UserNetworkSelection] uns ON uns.networkId = n.Id;
                END
            ");

            // Populate WebNetwork.Wanted from UserWebNetworkSelection
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserWebNetworkSelection')
                BEGIN
                    UPDATE wn
                    SET wn.Wanted = CAST(uwns.[include] AS BIT)
                    FROM [dbo].[WebNetwork] wn
                    INNER JOIN [dbo].[UserWebNetworkSelection] uwns ON uwns.webnetworkId = wn.Id;
                END
            ");

            // Populate Type.Wanted from UserTypeSelection
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserTypeSelection')
                BEGIN
                    UPDATE t
                    SET t.Wanted = CAST(uts.[include] AS BIT)
                    FROM [dbo].[Type] t
                    INNER JOIN [dbo].[UserTypeSelection] uts ON uts.typeId = t.Id;
                END
            ");

            // Drop defunct junction tables
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserCountrySelection')
                DROP TABLE [dbo].[UserCountrySelection];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserGenreSelection')
                DROP TABLE [dbo].[UserGenreSelection];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserLanguageSelection')
                DROP TABLE [dbo].[UserLanguageSelection];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserNetworkSelection')
                DROP TABLE [dbo].[UserNetworkSelection];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserWebNetworkSelection')
                DROP TABLE [dbo].[UserWebNetworkSelection];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserTypeSelection')
                DROP TABLE [dbo].[UserTypeSelection];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Country') AND name = 'Wanted')
                ALTER TABLE [dbo].[Country] DROP COLUMN [Wanted];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('GenreText') AND name = 'Wanted')
                ALTER TABLE [dbo].[GenreText] DROP COLUMN [Wanted];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Language') AND name = 'Wanted')
                ALTER TABLE [dbo].[Language] DROP COLUMN [Wanted];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Network') AND name = 'Wanted')
                ALTER TABLE [dbo].[Network] DROP COLUMN [Wanted];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('WebNetwork') AND name = 'Wanted')
                ALTER TABLE [dbo].[WebNetwork] DROP COLUMN [Wanted];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Type') AND name = 'Wanted')
                ALTER TABLE [dbo].[Type] DROP COLUMN [Wanted];
            ");
        }
    }
}
