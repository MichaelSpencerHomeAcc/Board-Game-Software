using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddGameAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardGameAlias",
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
                    AliasName = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    NormalizedAliasName = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    Source = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "manual"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_BoardGameAlias", x => x.ID);
                    table.ForeignKey(
                        name: "FK_bgd_BoardGameAlias__bgd_BoardGame",
                        column: x => x.FK_bgd_BoardGame,
                        principalSchema: "bgd",
                        principalTable: "BoardGame",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "AK_bgd_BoardGameAlias_GID",
                schema: "bgd",
                table: "BoardGameAlias",
                column: "GID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGameAlias_FK_bgd_BoardGame",
                schema: "bgd",
                table: "BoardGameAlias",
                column: "FK_bgd_BoardGame");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGameAlias_NormalizedAliasName",
                schema: "bgd",
                table: "BoardGameAlias",
                column: "NormalizedAliasName");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_BoardGameAlias_Game_NormalizedAlias",
                schema: "bgd",
                table: "BoardGameAlias",
                columns: new[] { "FK_bgd_BoardGame", "NormalizedAliasName" },
                unique: true,
                filter: "[Inactive] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardGameAlias",
                schema: "bgd");
        }
    }
}
