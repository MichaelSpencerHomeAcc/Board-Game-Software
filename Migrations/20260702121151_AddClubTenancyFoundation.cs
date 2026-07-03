using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddClubTenancyFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlayerBoardGame_FK_bgd_Player_Rank",
                schema: "bgd",
                table: "PlayerBoardGame");

            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "Player",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameNight",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Club",
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
                    ClubName = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Slug = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    VenueName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    VenueAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_Club", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ClubMembership",
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
                    FK_bgd_Club = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_ClubMembership", x => x.ID);
                    table.ForeignKey(
                        name: "FK_bgd_ClubMembership__bgd_Club",
                        column: x => x.FK_bgd_Club,
                        principalSchema: "bgd",
                        principalTable: "Club",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBoardGame_FK_bgd_Player_Rank",
                schema: "bgd",
                table: "PlayerBoardGame",
                columns: new[] { "FK_bgd_Player", "Rank" },
                unique: true,
                filter: "[FK_bgd_Player] IS NOT NULL AND [Inactive] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Player_FK_bgd_Club",
                schema: "bgd",
                table: "Player",
                column: "FK_bgd_Club");

            migrationBuilder.CreateIndex(
                name: "IX_BoardGameNight_FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameNight",
                column: "FK_bgd_Club");

            migrationBuilder.CreateIndex(
                name: "AK_bgd_Club_GID",
                schema: "bgd",
                table: "Club",
                column: "GID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_Club_Slug",
                schema: "bgd",
                table: "Club",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "AK_bgd_ClubMembership_GID",
                schema: "bgd",
                table: "ClubMembership",
                column: "GID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_ClubMembership_Club_User",
                schema: "bgd",
                table: "ClubMembership",
                columns: new[] { "FK_bgd_Club", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_BoardGameNight__bgd_Club",
                schema: "bgd",
                table: "BoardGameNight",
                column: "FK_bgd_Club",
                principalSchema: "bgd",
                principalTable: "Club",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_bgd_Player__bgd_Club",
                schema: "bgd",
                table: "Player",
                column: "FK_bgd_Club",
                principalSchema: "bgd",
                principalTable: "Club",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bgd_BoardGameNight__bgd_Club",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropForeignKey(
                name: "FK_bgd_Player__bgd_Club",
                schema: "bgd",
                table: "Player");

            migrationBuilder.DropTable(
                name: "ClubMembership",
                schema: "bgd");

            migrationBuilder.DropTable(
                name: "Club",
                schema: "bgd");

            migrationBuilder.DropIndex(
                name: "IX_PlayerBoardGame_FK_bgd_Player_Rank",
                schema: "bgd",
                table: "PlayerBoardGame");

            migrationBuilder.DropIndex(
                name: "IX_Player_FK_bgd_Club",
                schema: "bgd",
                table: "Player");

            migrationBuilder.DropIndex(
                name: "IX_BoardGameNight_FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBoardGame_FK_bgd_Player_Rank",
                schema: "bgd",
                table: "PlayerBoardGame",
                columns: new[] { "FK_bgd_Player", "Rank" },
                unique: true,
                filter: "[FK_bgd_Player] IS NOT NULL");

        }
    }
}
