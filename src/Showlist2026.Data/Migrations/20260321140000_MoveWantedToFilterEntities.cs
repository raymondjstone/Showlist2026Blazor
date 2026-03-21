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

            // Drop unused ShowUpdated table and its FK from Show
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Show_ShowUpdated_ShowUpdatedsId')
                ALTER TABLE [dbo].[Show] DROP CONSTRAINT [FK_Show_ShowUpdated_ShowUpdatedsId];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Show_ShowUpdatedsId' AND object_id = OBJECT_ID('Show'))
                DROP INDEX [IX_Show_ShowUpdatedsId] ON [dbo].[Show];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Show') AND name = 'ShowUpdatedsId')
                ALTER TABLE [dbo].[Show] DROP COLUMN [ShowUpdatedsId];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShowUpdated')
                DROP TABLE [dbo].[ShowUpdated];
            ");

            // Drop unused FK indexes
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Show_WebNetworksId' AND object_id = OBJECT_ID('Show'))
                DROP INDEX [IX_Show_WebNetworksId] ON [dbo].[Show];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Show_TypesId' AND object_id = OBJECT_ID('Show'))
                DROP INDEX [IX_Show_TypesId] ON [dbo].[Show];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Show_NetworksId' AND object_id = OBJECT_ID('Show'))
                DROP INDEX [IX_Show_NetworksId] ON [dbo].[Show];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Show_LanguagesId' AND object_id = OBJECT_ID('Show'))
                DROP INDEX [IX_Show_LanguagesId] ON [dbo].[Show];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Genre_genretextId' AND object_id = OBJECT_ID('Genre'))
                DROP INDEX [IX_Genre_genretextId] ON [dbo].[Genre];
            ");

            // Performance indexes
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Episode_Watched' AND object_id = OBJECT_ID('Episode'))
                CREATE NONCLUSTERED INDEX [IX_Episode_Watched]
                ON [dbo].[Episode] ([Watched]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Episode_Season_Number_AirDateOffset2_Comprehensive' AND object_id = OBJECT_ID('Episode'))
                CREATE NONCLUSTERED INDEX [IX_Episode_Season_Number_AirDateOffset2_Comprehensive]
                ON [dbo].[Episode] ([season], [number], [AirDateOffset2])
                INCLUDE ([episodeid], [name], [airdate], [airtime], [runtime], [summary], [links], [showId], [imagemedium], [imageoriginal], [Watched], [GivenUp]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Show_Wanted' AND object_id = OBJECT_ID('Show'))
                CREATE NONCLUSTERED INDEX [IX_Show_Wanted]
                ON [dbo].[Show] ([Wanted])
                INCLUDE ([showid]);
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
