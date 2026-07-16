using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridBoardGameMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_MergedIntoBoardGame",
                schema: "bgd",
                table: "BoardGame",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameSource",
                schema: "bgd",
                table: "BoardGame",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<string>(
                name: "GameStatus",
                schema: "bgd",
                table: "BoardGame",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "approved");

            migrationBuilder.AddColumn<string>(
                name: "LocalGameStatus",
                schema: "bgd",
                table: "BoardGame",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "bgd",
                table: "BoardGame",
                type: "varchar(120)",
                unicode: false,
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByUserId",
                schema: "bgd",
                table: "BoardGame",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [bgd].[BoardGame]
                SET
                    [NormalizedName] = LOWER(LTRIM(RTRIM([BoardGameName]))),
                    [GameSource] = CASE
                        WHEN [FK_bgd_Club] IS NULL THEN 'admin_created'
                        WHEN [FK_bgd_TemplateBoardGame] IS NULL THEN 'club_submitted'
                        ELSE 'licensed_import'
                    END,
                    [GameStatus] = CASE
                        WHEN [FK_bgd_Club] IS NOT NULL AND [FK_bgd_TemplateBoardGame] IS NULL THEN 'pending'
                        ELSE 'approved'
                    END,
                    [LocalGameStatus] = CASE
                        WHEN [FK_bgd_Club] IS NULL THEN NULL
                        WHEN [FK_bgd_TemplateBoardGame] IS NULL THEN 'local_only'
                        ELSE 'linked'
                    END
                WHERE [BoardGameName] IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGame_Club_LocalGameStatus",
                schema: "bgd",
                table: "BoardGame",
                columns: new[] { "FK_bgd_Club", "LocalGameStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGame_Club_NormalizedName",
                schema: "bgd",
                table: "BoardGame",
                columns: new[] { "FK_bgd_Club", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGame_FK_bgd_MergedIntoBoardGame",
                schema: "bgd",
                table: "BoardGame",
                column: "FK_bgd_MergedIntoBoardGame");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGame_Status_Source",
                schema: "bgd",
                table: "BoardGame",
                columns: new[] { "GameStatus", "GameSource" });

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGame__bgd_MergedIntoBoardGame",
                schema: "bgd",
                table: "BoardGame",
                column: "FK_bgd_MergedIntoBoardGame",
                principalSchema: "bgd",
                principalTable: "BoardGame",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGame__bgd_MergedIntoBoardGame",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGame_Club_LocalGameStatus",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGame_Club_NormalizedName",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGame_FK_bgd_MergedIntoBoardGame",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGame_Status_Source",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropColumn(
                name: "FK_bgd_MergedIntoBoardGame",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropColumn(
                name: "GameSource",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropColumn(
                name: "GameStatus",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropColumn(
                name: "LocalGameStatus",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "bgd",
                table: "BoardGame");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                schema: "bgd",
                table: "BoardGame");
        }
    }
}
