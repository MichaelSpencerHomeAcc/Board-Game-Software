using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAccountTier",
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
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SubscriptionTier = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "free_player"),
                    IsComped = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_UserAccountTier", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "AK_bgd_UserAccountTier_GID",
                schema: "bgd",
                table: "UserAccountTier",
                column: "GID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bgd_UserAccountTier_SubscriptionTier",
                schema: "bgd",
                table: "UserAccountTier",
                column: "SubscriptionTier");

            migrationBuilder.CreateIndex(
                name: "UQ_bgd_UserAccountTier_UserId",
                schema: "bgd",
                table: "UserAccountTier",
                column: "UserId",
                unique: true,
                filter: "[Inactive] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAccountTier",
                schema: "bgd");
        }
    }
}
