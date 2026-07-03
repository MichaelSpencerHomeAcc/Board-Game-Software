using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddClubScopedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_bgd_BoardGameMarkerType_TypeDesc",
                schema: "bgd",
                table: "BoardGameMarkerType");

            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "Publisher",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameMarkerType",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bgd_Publisher_Club_PublisherName",
                schema: "bgd",
                table: "Publisher",
                columns: new[] { "FK_bgd_Club", "PublisherName" },
                filter: "[FK_bgd_Club] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_Publisher_Global_PublisherName",
                schema: "bgd",
                table: "Publisher",
                column: "PublisherName",
                filter: "[FK_bgd_Club] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_BoardGameMarkerType_Club_TypeDesc",
                schema: "bgd",
                table: "BoardGameMarkerType",
                columns: new[] { "FK_bgd_Club", "TypeDesc" },
                unique: true,
                filter: "[FK_bgd_Club] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_BoardGameMarkerType_Global_TypeDesc",
                schema: "bgd",
                table: "BoardGameMarkerType",
                column: "TypeDesc",
                unique: true,
                filter: "[FK_bgd_Club] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameMarkerType__bgd_Club",
                schema: "bgd",
                table: "BoardGameMarkerType",
                column: "FK_bgd_Club",
                principalSchema: "bgd",
                principalTable: "Club",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_Publisher__bgd_Club",
                schema: "bgd",
                table: "Publisher",
                column: "FK_bgd_Club",
                principalSchema: "bgd",
                principalTable: "Club",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameMarkerType__bgd_Club",
                schema: "bgd",
                table: "BoardGameMarkerType");

            migrationBuilder.DropForeignKey(
                name: "FK_bgd_Publisher__bgd_Club",
                schema: "bgd",
                table: "Publisher");

            migrationBuilder.DropIndex(
                name: "UQ_bgd_BoardGameMarkerType_Club_TypeDesc",
                schema: "bgd",
                table: "BoardGameMarkerType");

            migrationBuilder.DropIndex(
                name: "UQ_bgd_BoardGameMarkerType_Global_TypeDesc",
                schema: "bgd",
                table: "BoardGameMarkerType");

            migrationBuilder.DropIndex(
                name: "IX_bgd_Publisher_Club_PublisherName",
                schema: "bgd",
                table: "Publisher");

            migrationBuilder.DropIndex(
                name: "IX_bgd_Publisher_Global_PublisherName",
                schema: "bgd",
                table: "Publisher");

            migrationBuilder.DropColumn(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameMarkerType");

            migrationBuilder.DropColumn(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "Publisher");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_BoardGameMarkerType_TypeDesc",
                schema: "bgd",
                table: "BoardGameMarkerType",
                column: "TypeDesc",
                unique: true);
        }
    }
}
