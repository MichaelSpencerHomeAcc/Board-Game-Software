using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerClubMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerClub",
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
                    FK_bgd_Club = table.Column<long>(type: "bigint", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_PlayerClub", x => x.ID);
                    table.ForeignKey(
                        name: "FK_bgd_PlayerClub__bgd_Club",
                        column: x => x.FK_bgd_Club,
                        principalSchema: "bgd",
                        principalTable: "Club",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_bgd_PlayerClub__bgd_Player",
                        column: x => x.FK_bgd_Player,
                        principalSchema: "bgd",
                        principalTable: "Player",
                        principalColumn: "ID");
                });

            migrationBuilder.Sql("""
                INSERT INTO [bgd].[PlayerClub]
                    ([GID], [Inactive], [CreatedBy], [TimeCreated], [ModifiedBy], [TimeModified], [FK_bgd_Player], [FK_bgd_Club], [JoinedAt])
                SELECT
                    NEWID(),
                    0,
                    COALESCE(NULLIF([CreatedBy], N''), N'EF migration'),
                    COALESCE([TimeCreated], GETUTCDATE()),
                    COALESCE(NULLIF([ModifiedBy], N''), N'EF migration'),
                    GETUTCDATE(),
                    [ID],
                    [FK_bgd_Club],
                    GETUTCDATE()
                FROM [bgd].[Player]
                WHERE [FK_bgd_Club] IS NOT NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [bgd].[PlayerClub] pc
                        WHERE pc.[FK_bgd_Player] = [bgd].[Player].[ID]
                            AND pc.[FK_bgd_Club] = [bgd].[Player].[FK_bgd_Club]
                    );
                """);

            migrationBuilder.CreateIndex(
                name: "AK_bgd_PlayerClub_GID",
                schema: "bgd",
                table: "PlayerClub",
                column: "GID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerClub_FK_bgd_Club",
                schema: "bgd",
                table: "PlayerClub",
                column: "FK_bgd_Club");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_PlayerClub_Player_Club",
                schema: "bgd",
                table: "PlayerClub",
                columns: new[] { "FK_bgd_Player", "FK_bgd_Club" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerClub",
                schema: "bgd");
        }
    }
}
