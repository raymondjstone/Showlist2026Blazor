using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserGivenUpSelectionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserGivenUpSelection')
                CREATE TABLE [dbo].[UserGivenUpSelection] (
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [episodeId] INT NULL,
                    [GivenUpDate] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                    CONSTRAINT [PK_UserGivenUpSelection] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_UserGivenUpSelection_Episode_episodeId] FOREIGN KEY ([episodeId]) REFERENCES [dbo].[Episode] ([Id])
                );
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserGivenUpSelection_episodeId' AND object_id = OBJECT_ID('UserGivenUpSelection'))
                CREATE INDEX [IX_UserGivenUpSelection_episodeId] ON [dbo].[UserGivenUpSelection] ([episodeId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [dbo].[UserGivenUpSelection];");
        }
    }
}
