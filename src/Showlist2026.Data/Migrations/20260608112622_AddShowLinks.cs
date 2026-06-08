using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Showlist2026.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShowLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShowLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PredecessorShowId = table.Column<int>(type: "int", nullable: false),
                    SuccessorShowId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShowLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShowLink_Show_PredecessorShowId",
                        column: x => x.PredecessorShowId,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShowLink_Show_SuccessorShowId",
                        column: x => x.SuccessorShowId,
                        principalTable: "Show",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShowLink_PredecessorShowId",
                table: "ShowLink",
                column: "PredecessorShowId");

            migrationBuilder.CreateIndex(
                name: "IX_ShowLink_SuccessorShowId",
                table: "ShowLink",
                column: "SuccessorShowId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShowLink");
        }
    }
}
