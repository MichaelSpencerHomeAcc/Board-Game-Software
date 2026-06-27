using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_BoardGame",
                schema: "bgd",
                table: "BoardGameVote");

            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_BoardGameNight",
                schema: "bgd",
                table: "BoardGameVote");

            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_Player",
                schema: "bgd",
                table: "BoardGameVote");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_BoardGame",
                schema: "bgd",
                table: "BoardGameVote",
                column: "FK_bgd_BoardGame",
                principalSchema: "bgd",
                principalTable: "BoardGame",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_BoardGameNight",
                schema: "bgd",
                table: "BoardGameVote",
                column: "FK_bgd_BoardGameNight",
                principalSchema: "bgd",
                principalTable: "BoardGameNight",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_Player",
                schema: "bgd",
                table: "BoardGameVote",
                column: "FK_bgd_Player",
                principalSchema: "bgd",
                principalTable: "Player",
                principalColumn: "ID");

            migrationBuilder.CreateTable(
                name: "PlayerAchievement",
                schema: "bgd",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    Inactive = table.Column<bool>(type: "bit", nullable: false),
                    VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
                    FK_bgd_Player = table.Column<long>(type: "bigint", nullable: false),
                    BadgeCode = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    BadgeTitle = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BadgeDetail = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: true),
                    FK_bgd_BoardGameMatch = table.Column<long>(type: "bigint", nullable: true),
                    FK_bgd_BoardGameNight = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_PlayerAchievement", x => x.ID);
                    table.ForeignKey(
                        name: "FK_bgd_PlayerAchievement__bgd_BoardGame",
                        column: x => x.FK_bgd_BoardGame,
                        principalSchema: "bgd",
                        principalTable: "BoardGame",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_bgd_PlayerAchievement__bgd_BoardGameMatch",
                        column: x => x.FK_bgd_BoardGameMatch,
                        principalSchema: "bgd",
                        principalTable: "BoardGameMatch",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_bgd_PlayerAchievement__bgd_BoardGameNight",
                        column: x => x.FK_bgd_BoardGameNight,
                        principalSchema: "bgd",
                        principalTable: "BoardGameNight",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_bgd_PlayerAchievement__bgd_Player",
                        column: x => x.FK_bgd_Player,
                        principalSchema: "bgd",
                        principalTable: "Player",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "AK_bgd_PlayerAchievement_GID",
                schema: "bgd",
                table: "PlayerAchievement",
                column: "GID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievement_FK_bgd_BoardGame",
                schema: "bgd",
                table: "PlayerAchievement",
                column: "FK_bgd_BoardGame");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievement_FK_bgd_BoardGameMatch",
                schema: "bgd",
                table: "PlayerAchievement",
                column: "FK_bgd_BoardGameMatch");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievement_FK_bgd_BoardGameNight",
                schema: "bgd",
                table: "PlayerAchievement",
                column: "FK_bgd_BoardGameNight");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_PlayerAchievement_Scope",
                schema: "bgd",
                table: "PlayerAchievement",
                columns: new[] { "FK_bgd_Player", "BadgeCode", "FK_bgd_BoardGame", "FK_bgd_BoardGameMatch", "FK_bgd_BoardGameNight" },
                unique: true,
                filter: "[FK_bgd_BoardGame] IS NOT NULL AND [FK_bgd_BoardGameMatch] IS NOT NULL AND [FK_bgd_BoardGameNight] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerAchievement",
                schema: "bgd");

            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_BoardGame",
                schema: "bgd",
                table: "BoardGameVote");

            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_BoardGameNight",
                schema: "bgd",
                table: "BoardGameVote");

            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_Player",
                schema: "bgd",
                table: "BoardGameVote");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_BoardGame",
                schema: "bgd",
                table: "BoardGameVote",
                column: "FK_bgd_BoardGame",
                principalSchema: "bgd",
                principalTable: "BoardGame",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_BoardGameNight",
                schema: "bgd",
                table: "BoardGameVote",
                column: "FK_bgd_BoardGameNight",
                principalSchema: "bgd",
                principalTable: "BoardGameNight",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameVote__bgd_Player",
                schema: "bgd",
                table: "BoardGameVote",
                column: "FK_bgd_Player",
                principalSchema: "bgd",
                principalTable: "Player",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
