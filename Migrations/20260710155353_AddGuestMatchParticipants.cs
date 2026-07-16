using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestMatchParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "FK_bgd_Player",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "CharacterName",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Colour",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Faction",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitedEmail",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Placement",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "decimal(9,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamId",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Won",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "bit",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_bgd_BoardGameMatchPlayer_PlayerOrGuest",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                sql: "[FK_bgd_Player] IS NOT NULL OR [GuestName] IS NOT NULL OR [InvitedEmail] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_bgd_BoardGameMatchPlayer_PlayerOrGuest",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "CharacterName",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "Colour",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "Faction",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "GuestName",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "InvitedEmail",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "Placement",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "Score",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "TeamId",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.DropColumn(
                name: "Won",
                schema: "bgd",
                table: "BoardGameMatchPlayer");

            migrationBuilder.AlterColumn<long>(
                name: "FK_bgd_Player",
                schema: "bgd",
                table: "BoardGameMatchPlayer",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
