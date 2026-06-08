using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonOffsetToShowFolderAlias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeasonOffset",
                table: "ShowFolderAliases",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeasonOffset",
                table: "ShowFolderAliases");
        }
    }
}
