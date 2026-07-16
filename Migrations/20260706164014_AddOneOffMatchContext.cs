using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddOneOffMatchContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameMatch",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlayContext",
                schema: "bgd",
                table: "BoardGameMatch",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "club_game_night");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                schema: "bgd",
                table: "BoardGameMatch",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "members_only");

            migrationBuilder.Sql("""
                UPDATE m
                SET
                    m.[FK_bgd_Club] = n.[FK_bgd_Club],
                    m.[PlayContext] = 'club_game_night',
                    m.[Visibility] = CASE
                        WHEN c.[ClubType] = 'private_group' THEN 'private'
                        ELSE 'members_only'
                    END
                FROM [bgd].[BoardGameMatch] m
                INNER JOIN [bgd].[BoardGameNightBoardGameMatch] link
                    ON link.[FK_bgd_BoardGameMatch] = m.[ID]
                    AND link.[Inactive] = 0
                INNER JOIN [bgd].[BoardGameNight] n
                    ON n.[ID] = link.[FK_bgd_BoardGameNight]
                LEFT JOIN [bgd].[Club] c
                    ON c.[ID] = n.[FK_bgd_Club];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGameMatch_Context_Visibility",
                schema: "bgd",
                table: "BoardGameMatch",
                columns: new[] { "PlayContext", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGameMatch_FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameMatch",
                column: "FK_bgd_Club");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameMatch__bgd_Club",
                schema: "bgd",
                table: "BoardGameMatch",
                column: "FK_bgd_Club",
                principalSchema: "bgd",
                principalTable: "Club",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameMatch__bgd_Club",
                schema: "bgd",
                table: "BoardGameMatch");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGameMatch_Context_Visibility",
                schema: "bgd",
                table: "BoardGameMatch");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGameMatch_FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameMatch");

            migrationBuilder.DropColumn(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameMatch");

            migrationBuilder.DropColumn(
                name: "PlayContext",
                schema: "bgd",
                table: "BoardGameMatch");

            migrationBuilder.DropColumn(
                name: "Visibility",
                schema: "bgd",
                table: "BoardGameMatch");
        }
    }
}
