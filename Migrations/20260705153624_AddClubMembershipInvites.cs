using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddClubMembershipInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_bgd_ClubMembership_Club_User",
                schema: "bgd",
                table: "ClubMembership");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "bgd",
                table: "ClubMembership",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                schema: "bgd",
                table: "ClubMembership",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitedByUserId",
                schema: "bgd",
                table: "ClubMembership",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitedEmail",
                schema: "bgd",
                table: "ClubMembership",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "bgd",
                table: "ClubMembership",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "active");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_ClubMembership_Club_InvitedEmail",
                schema: "bgd",
                table: "ClubMembership",
                columns: new[] { "FK_bgd_Club", "InvitedEmail" },
                filter: "[InvitedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_ClubMembership_Club_User",
                schema: "bgd",
                table: "ClubMembership",
                columns: new[] { "FK_bgd_Club", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bgd_ClubMembership_Club_InvitedEmail",
                schema: "bgd",
                table: "ClubMembership");

            migrationBuilder.DropIndex(
                name: "UQ_bgd_ClubMembership_Club_User",
                schema: "bgd",
                table: "ClubMembership");

            migrationBuilder.DropColumn(
                name: "GuestName",
                schema: "bgd",
                table: "ClubMembership");

            migrationBuilder.DropColumn(
                name: "InvitedByUserId",
                schema: "bgd",
                table: "ClubMembership");

            migrationBuilder.DropColumn(
                name: "InvitedEmail",
                schema: "bgd",
                table: "ClubMembership");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "bgd",
                table: "ClubMembership");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "bgd",
                table: "ClubMembership",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_ClubMembership_Club_User",
                schema: "bgd",
                table: "ClubMembership",
                columns: new[] { "FK_bgd_Club", "UserId" },
                unique: true);
        }
    }
}
