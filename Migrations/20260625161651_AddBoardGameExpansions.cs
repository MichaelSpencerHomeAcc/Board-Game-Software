using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardGameExpansions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExpansion",
                schema: "bgd",
                table: "BoardGame",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BoardGameExpansion",
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
                    FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: false),
                    FK_bgd_ExpansionBoardGame = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_BoardGameExpansion", x => x.ID);
                    table.ForeignKey(
                        name: "FK_bgd_BoardGameExpansion__bgd_BoardGame",
                        column: x => x.FK_bgd_BoardGame,
                        principalSchema: "bgd",
                        principalTable: "BoardGame",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_bgd_BoardGameExpansion__bgd_ExpansionBoardGame",
                        column: x => x.FK_bgd_ExpansionBoardGame,
                        principalSchema: "bgd",
                        principalTable: "BoardGame",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "AK_bgd_BoardGameExpansion_GID",
                schema: "bgd",
                table: "BoardGameExpansion",
                column: "GID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardGameExpansion_FK_bgd_ExpansionBoardGame",
                schema: "bgd",
                table: "BoardGameExpansion",
                column: "FK_bgd_ExpansionBoardGame");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_BoardGameExpansion_FK_bgd_BoardGame_FK_bgd_ExpansionBoardGame",
                schema: "bgd",
                table: "BoardGameExpansion",
                columns: new[] { "FK_bgd_BoardGame", "FK_bgd_ExpansionBoardGame" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardGameExpansion",
                schema: "bgd");

            migrationBuilder.DropColumn(
                name: "IsExpansion",
                schema: "bgd",
                table: "BoardGame");
        }
    }
}
