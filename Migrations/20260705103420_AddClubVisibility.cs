using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddClubVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowJoinRequests",
                schema: "bgd",
                table: "Club",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ClubType",
                schema: "bgd",
                table: "Club",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "public_club");

            migrationBuilder.AddColumn<string>(
                name: "DefaultGameNightVisibility",
                schema: "bgd",
                table: "Club",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "public");

            migrationBuilder.AddColumn<string>(
                name: "DefaultMatchVisibility",
                schema: "bgd",
                table: "Club",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "public");

            migrationBuilder.AddColumn<bool>(
                name: "IsDiscoverable",
                schema: "bgd",
                table: "Club",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowStatsPublicly",
                schema: "bgd",
                table: "Club",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                schema: "bgd",
                table: "Club",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "public");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_Club_Discovery",
                schema: "bgd",
                table: "Club",
                columns: new[] { "IsDiscoverable", "Visibility" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bgd_Club_Discovery",
                schema: "bgd",
                table: "Club");

            migrationBuilder.DropColumn(
                name: "AllowJoinRequests",
                schema: "bgd",
                table: "Club");

            migrationBuilder.DropColumn(
                name: "ClubType",
                schema: "bgd",
                table: "Club");

            migrationBuilder.DropColumn(
                name: "DefaultGameNightVisibility",
                schema: "bgd",
                table: "Club");

            migrationBuilder.DropColumn(
                name: "DefaultMatchVisibility",
                schema: "bgd",
                table: "Club");

            migrationBuilder.DropColumn(
                name: "IsDiscoverable",
                schema: "bgd",
                table: "Club");

            migrationBuilder.DropColumn(
                name: "ShowStatsPublicly",
                schema: "bgd",
                table: "Club");

            migrationBuilder.DropColumn(
                name: "Visibility",
                schema: "bgd",
                table: "Club");
        }
    }
}
