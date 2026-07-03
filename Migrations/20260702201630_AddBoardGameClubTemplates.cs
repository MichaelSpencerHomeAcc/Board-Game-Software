using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardGameClubTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "BoardGame",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_TemplateBoardGame",
                schema: "bgd",
                table: "BoardGame",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGame_FK_bgd_Club",
                schema: "bgd",
                table: "BoardGame",
                column: "FK_bgd_Club");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGame_FK_bgd_TemplateBoardGame",
                schema: "bgd",
                table: "BoardGame",
                column: "FK_bgd_TemplateBoardGame");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGame__bgd_Club",
                schema: "bgd",
                table: "BoardGame",
                column: "FK_bgd_Club",
                principalSchema: "bgd",
                principalTable: "Club",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGame__bgd_TemplateBoardGame",
                schema: "bgd",
                table: "BoardGame",
                column: "FK_bgd_TemplateBoardGame",
                principalSchema: "bgd",
                principalTable: "BoardGame",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGame__bgd_Club",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGame__bgd_TemplateBoardGame",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGame_FK_bgd_Club",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGame_FK_bgd_TemplateBoardGame",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropColumn(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropColumn(
                name: "FK_bgd_TemplateBoardGame",
                schema: "bgd",
                table: "BoardGame");
        }
    }
}
