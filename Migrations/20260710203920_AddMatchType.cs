using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchType",
                schema: "bgd",
                table: "BoardGameMatch",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "scored_match");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGameMatch_Type_Complete",
                schema: "bgd",
                table: "BoardGameMatch",
                columns: new[] { "MatchType", "MatchComplete" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGameMatch_Type_Complete",
                schema: "bgd",
                table: "BoardGameMatch");

            migrationBuilder.DropColumn(
                name: "MatchType",
                schema: "bgd",
                table: "BoardGameMatch");
        }
    }
}
