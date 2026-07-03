using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddShelfClubOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "Shelf",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bgd_Shelf_FK_bgd_Club",
                schema: "bgd",
                table: "Shelf",
                column: "FK_bgd_Club");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_Shelf__bgd_Club",
                schema: "bgd",
                table: "Shelf",
                column: "FK_bgd_Club",
                principalSchema: "bgd",
                principalTable: "Club",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bgd_Shelf__bgd_Club",
                schema: "bgd",
                table: "Shelf");

            migrationBuilder.DropIndex(
                name: "IX_bgd_Shelf_FK_bgd_Club",
                schema: "bgd",
                table: "Shelf");

            migrationBuilder.DropColumn(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "Shelf");
        }
    }
}
