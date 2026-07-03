using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredImage",
                schema: "bgd",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    BlobProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BlobKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PublicUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(127)", maxLength: 127, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bgd_StoredImage", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bgd_StoredImage_BlobKey",
                schema: "bgd",
                table: "StoredImage",
                column: "BlobKey");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_StoredImage_CreatedAtUtc",
                schema: "bgd",
                table: "StoredImage",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_bgd_StoredImage_OwnerType_OwnerId",
                schema: "bgd",
                table: "StoredImage",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_bgd_StoredImage_UploadedByUserId",
                schema: "bgd",
                table: "StoredImage",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredImage",
                schema: "bgd");
        }
    }
}
