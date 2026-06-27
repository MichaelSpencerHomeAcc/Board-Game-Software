using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardGameVotes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardGameVote",
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
                    FK_bgd_BoardGameNight = table.Column<long>(type: "bigint", nullable: false),
                    FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: false),
                    FK_bgd_Player = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_BoardGameVote", x => x.ID);
                    table.ForeignKey(
                        name: "FK_bgd_BoardGameVote__bgd_BoardGame",
                        column: x => x.FK_bgd_BoardGame,
                        principalSchema: "bgd",
                        principalTable: "BoardGame",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_bgd_BoardGameVote__bgd_BoardGameNight",
                        column: x => x.FK_bgd_BoardGameNight,
                        principalSchema: "bgd",
                        principalTable: "BoardGameNight",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_bgd_BoardGameVote__bgd_Player",
                        column: x => x.FK_bgd_Player,
                        principalSchema: "bgd",
                        principalTable: "Player",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "AK_bgd_BoardGameVote_GID",
                schema: "bgd",
                table: "BoardGameVote",
                column: "GID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardGameVote_FK_bgd_BoardGame",
                schema: "bgd",
                table: "BoardGameVote",
                column: "FK_bgd_BoardGame");

            migrationBuilder.CreateIndex(
                name: "IX_BoardGameVote_FK_bgd_Player",
                schema: "bgd",
                table: "BoardGameVote",
                column: "FK_bgd_Player");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_BoardGameVote_Night_Game_Player",
                schema: "bgd",
                table: "BoardGameVote",
                columns: new[] { "FK_bgd_BoardGameNight", "FK_bgd_BoardGame", "FK_bgd_Player" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardGameVote",
                schema: "bgd");
        }
    }
}
