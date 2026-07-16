using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddGameNightVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BoardGameNight_FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.AddColumn<string>(
                name: "BookingUrl",
                schema: "bgd",
                table: "BoardGameNight",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                schema: "bgd",
                table: "BoardGameNight",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "bgd",
                table: "BoardGameNight",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                schema: "bgd",
                table: "BoardGameNight",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LocationId",
                schema: "bgd",
                table: "BoardGameNight",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                schema: "bgd",
                table: "BoardGameNight",
                type: "datetime",
                nullable: false,
                defaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "bgd",
                table: "BoardGameNight",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                schema: "bgd",
                table: "BoardGameNight",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "members_only");

            migrationBuilder.Sql("""
                UPDATE gn
                SET
                    [StartsAt] = CAST(gn.[GameNightDate] AS datetime),
                    [Visibility] =
                        CASE
                            WHEN c.[ClubType] = 'private_group' THEN 'private'
                            WHEN c.[DefaultGameNightVisibility] IN ('public', 'members_only', 'private') THEN c.[DefaultGameNightVisibility]
                            ELSE 'members_only'
                        END
                FROM [bgd].[BoardGameNight] gn
                LEFT JOIN [bgd].[Club] c ON c.[ID] = gn.[FK_bgd_Club];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGameNight_Club_StartsAt",
                schema: "bgd",
                table: "BoardGameNight",
                columns: new[] { "FK_bgd_Club", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_bgd_BoardGameNight_Club_Visibility",
                schema: "bgd",
                table: "BoardGameNight",
                columns: new[] { "FK_bgd_Club", "Visibility" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGameNight_Club_StartsAt",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropIndex(
                name: "IX_bgd_BoardGameNight_Club_Visibility",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "BookingUrl",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.DropColumn(
                name: "Visibility",
                schema: "bgd",
                table: "BoardGameNight");

            migrationBuilder.CreateIndex(
                name: "IX_BoardGameNight_FK_bgd_Club",
                schema: "bgd",
                table: "BoardGameNight",
                column: "FK_bgd_Club");
        }
    }
}
