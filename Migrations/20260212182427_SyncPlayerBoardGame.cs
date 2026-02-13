using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class SyncPlayerBoardGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        //    migrationBuilder.EnsureSchema(
        //        name: "bgd");

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameImageType",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            TypeDesc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
        //            CustomSort = table.Column<int>(type: "int", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameImageType", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameNight",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            GameNightDate = table.Column<DateOnly>(type: "date", nullable: false),
        //            Finished = table.Column<bool>(type: "bit", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameNight", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameType",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            TypeDesc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
        //            CustomSort = table.Column<int>(type: "int", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameType", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameVictoryConditionType",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            TypeDesc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
        //            CustomSort = table.Column<int>(type: "int", nullable: true),
        //            Points = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
        //            WinLose = table.Column<bool>(type: "bit", nullable: true, defaultValue: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameVictoryConditionType", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "EloMethod",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            MethodName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            MethodDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_EloMethod", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "IdentityUser",
        //        columns: table => new
        //        {
        //            Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
        //            UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
        //            PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
        //            TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
        //            LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
        //            LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
        //            AccessFailedCount = table.Column<int>(type: "int", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_IdentityUser", x => x.Id);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "MarkerAdditionalType",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            TypeDesc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
        //            CustomSort = table.Column<int>(type: "int", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_MarkerAdditionalType", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "MarkerAlignmentType",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            TypeDesc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
        //            CustomSort = table.Column<int>(type: "int", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_MarkerAlignmentType", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "PlayerNameResults",
        //        columns: table => new
        //        {
        //            FullName = table.Column<string>(type: "nvarchar(max)", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "Publisher",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            PublisherName = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
        //            Description = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_Publisher", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "RankingQueryStore",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            RankingQueryStoreName = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
        //            ViewName = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_RankingQueryStore", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "ReleaseVersion",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            DBMajor = table.Column<byte>(type: "tinyint", nullable: true),
        //            DBMinor = table.Column<byte>(type: "tinyint", nullable: true),
        //            DBRevision = table.Column<byte>(type: "tinyint", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_ReleaseVersion", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "ResultType",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            TypeDesc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
        //            IsVictory = table.Column<bool>(type: "bit", nullable: false),
        //            IsDefeat = table.Column<bool>(type: "bit", nullable: false),
        //            CustomSort = table.Column<int>(type: "int", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_ResultType", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "Shelf",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ShelfName = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false),
        //            TotalRows = table.Column<byte>(type: "tinyint", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_Shelf", x => x.ID);
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "Player",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FirstName = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
        //            MiddleName = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
        //            LastName = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
        //            DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
        //            FKdboAspNetUsers = table.Column<string>(type: "varchar(450)", unicode: false, maxLength: 450, nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_Player", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_Player_AspNetUsers",
        //                column: x => x.FKdboAspNetUsers,
        //                principalTable: "IdentityUser",
        //                principalColumn: "Id");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameMarkerType",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            TypeDesc = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
        //            CustomSort = table.Column<int>(type: "int", nullable: true),
        //            FK_bgd_MarkerAlignmentType = table.Column<long>(type: "bigint", nullable: true),
        //            ImageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
        //            FK_bgd_MarkerAdditionalType = table.Column<long>(type: "bigint", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameMarkerType", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMarkerType__bgd_MarkerAdditionalType",
        //                column: x => x.FK_bgd_MarkerAdditionalType,
        //                principalSchema: "bgd",
        //                principalTable: "MarkerAdditionalType",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMarketType__bgd_MarkerAlignmentType",
        //                column: x => x.FK_bgd_MarkerAlignmentType,
        //                principalSchema: "bgd",
        //                principalTable: "MarkerAlignmentType",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGame",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            BoardGameName = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
        //            FK_bgd_BoardGameType = table.Column<long>(type: "bigint", nullable: true),
        //            FK_bgd_BoardGameVictoryConditionType = table.Column<long>(type: "bigint", nullable: true),
        //            FK_bgd_Publisher = table.Column<long>(type: "bigint", nullable: true),
        //            PlayerCountMin = table.Column<byte>(type: "tinyint", nullable: true),
        //            PlayerCountMax = table.Column<byte>(type: "tinyint", nullable: true),
        //            PlayingTimeMinInMinutes = table.Column<byte>(type: "tinyint", nullable: true),
        //            PlayingTimeMaxInMinutes = table.Column<byte>(type: "tinyint", nullable: true),
        //            ComplexityRating = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
        //            ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
        //            HasMarkers = table.Column<bool>(type: "bit", nullable: false),
        //            HeightCm = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
        //            WidthCm = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
        //            BoardGameSummary = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true),
        //            HowToPlayHyperlink = table.Column<string>(type: "varchar(max)", unicode: false, nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGame", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGame__bgd_BoardGameType",
        //                column: x => x.FK_bgd_BoardGameType,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameType",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGame__bgd_BoardGameVictoryConditionType",
        //                column: x => x.FK_bgd_BoardGameVictoryConditionType,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameVictoryConditionType",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGame__bgd_Publisher",
        //                column: x => x.FK_bgd_Publisher,
        //                principalSchema: "bgd",
        //                principalTable: "Publisher",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "ShelfSection",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_Shelf = table.Column<long>(type: "bigint", nullable: false),
        //            RowNumber = table.Column<byte>(type: "tinyint", nullable: false),
        //            SectionNumber = table.Column<byte>(type: "tinyint", nullable: false),
        //            HeightCm = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
        //            WidthCm = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
        //            Blocked = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
        //            SectionName = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_ShelfSection", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_ShelfSection__bgd_Shelf",
        //                column: x => x.FK_bgd_Shelf,
        //                principalSchema: "bgd",
        //                principalTable: "Shelf",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameNightPlayer",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGameNight = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_Player = table.Column<long>(type: "bigint", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameNightPlayer", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameNightPlayer__bgd_BoardGameNight",
        //                column: x => x.FK_bgd_BoardGameNight,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameNight",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameNightPlayer__bgd_Player",
        //                column: x => x.FK_bgd_Player,
        //                principalSchema: "bgd",
        //                principalTable: "Player",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameEloMethod",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_EloMethod = table.Column<long>(type: "bigint", nullable: false),
        //            ExpectedWinRatioTeamA = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
        //            Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameEloMethod", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameEloMethod__bgd_BoardGame",
        //                column: x => x.FK_bgd_BoardGame,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGame",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameEloMethod__bgd_EloMethod",
        //                column: x => x.FK_bgd_EloMethod,
        //                principalSchema: "bgd",
        //                principalTable: "EloMethod",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameMarker",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_BoardGameMarkerType = table.Column<long>(type: "bigint", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameMarker", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMarker__bgd_BoardGame",
        //                column: x => x.FK_bgd_BoardGame,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGame",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMarker__bgd_BoardGameMarkerType",
        //                column: x => x.FK_bgd_BoardGameMarkerType,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameMarkerType",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameMatch",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: false),
        //            MatchDate = table.Column<DateOnly>(type: "date", nullable: true),
        //            FK_bgd_ResultType = table.Column<long>(type: "bigint", nullable: true),
        //            FinishedDate = table.Column<DateOnly>(type: "date", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameMatch", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMatch__bgd_BoardGame",
        //                column: x => x.FK_bgd_BoardGame,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGame",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMatch__bgd_ResultType",
        //                column: x => x.FK_bgd_ResultType,
        //                principalSchema: "bgd",
        //                principalTable: "ResultType",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameResult",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_ResultType = table.Column<long>(type: "bigint", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameResult", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameResult__bgd_BoardGame",
        //                column: x => x.FK_bgd_BoardGame,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGame",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameResult__bgd_ResultType",
        //                column: x => x.FK_bgd_ResultType,
        //                principalSchema: "bgd",
        //                principalTable: "ResultType",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "PlayerBoardGame",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime2", nullable: false),
        //            FK_bgd_Player = table.Column<long>(type: "bigint", nullable: true),
        //            FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: true),
        //            Rank = table.Column<short>(type: "smallint", nullable: false),
        //            PlayerId = table.Column<long>(type: "bigint", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_PlayerBoardGame", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_PlayerBoardGame_BoardGame_FK_bgd_BoardGame",
        //                column: x => x.FK_bgd_BoardGame,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGame",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_PlayerBoardGame_Player_FK_bgd_Player",
        //                column: x => x.FK_bgd_Player,
        //                principalSchema: "bgd",
        //                principalTable: "Player",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_PlayerBoardGame_Player_PlayerId",
        //                column: x => x.PlayerId,
        //                principalSchema: "bgd",
        //                principalTable: "Player",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "PlayerBoardGameRating",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_Player = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: false),
        //            Rating = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
        //            Constant = table.Column<int>(type: "int", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_PlayerBoardGameRating", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_PlayerBoardGameRating__bgd_BoardGame",
        //                column: x => x.FK_bgd_BoardGame,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGame",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_PlayerBoardGameRating__bgd_Player",
        //                column: x => x.FK_bgd_Player,
        //                principalSchema: "bgd",
        //                principalTable: "Player",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameShelfSection",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGame = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_ShelfSection = table.Column<long>(type: "bigint", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameShelfSection", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameShelfSection__bgd_BoardGame",
        //                column: x => x.FK_bgd_BoardGame,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGame",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameShelfSection__bgd_ShelfSection",
        //                column: x => x.FK_bgd_ShelfSection,
        //                principalSchema: "bgd",
        //                principalTable: "ShelfSection",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameMatchPlayer",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGameMatch = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_Player = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_BoardGameMarker = table.Column<long>(type: "bigint", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameMatchPlayer", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMatchPlayer__bgd_BoardGameMarker",
        //                column: x => x.FK_bgd_BoardGameMarker,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameMarker",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMatchPlayer__bgd_BoardGameMatch",
        //                column: x => x.FK_bgd_BoardGameMatch,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameMatch",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMatchPlayer__bgd_Player",
        //                column: x => x.FK_bgd_Player,
        //                principalSchema: "bgd",
        //                principalTable: "Player",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameNightBoardGameMatch",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGameNight = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_BoardGameMatch = table.Column<long>(type: "bigint", nullable: false)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameNightBoardGameMatch", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameNightBoardGameMatch__bgd_BoardGameMatch",
        //                column: x => x.FK_bgd_BoardGameMatch,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameMatch",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameNightBoardGameMatch__bgd_BoardGameNight",
        //                column: x => x.FK_bgd_BoardGameNight,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameNight",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateTable(
        //        name: "BoardGameMatchPlayerResult",
        //        schema: "bgd",
        //        columns: table => new
        //        {
        //            ID = table.Column<long>(type: "bigint", nullable: false)
        //                .Annotation("SqlServer:Identity", "1, 1"),
        //            GID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
        //            Inactive = table.Column<bool>(type: "bit", nullable: false),
        //            VersionStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
        //            CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeCreated = table.Column<DateTime>(type: "datetime", nullable: false),
        //            ModifiedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
        //            TimeModified = table.Column<DateTime>(type: "datetime", nullable: false),
        //            FK_bgd_BoardGameMatchPlayer = table.Column<long>(type: "bigint", nullable: false),
        //            FK_bgd_ResultType = table.Column<long>(type: "bigint", nullable: true),
        //            FinalScore = table.Column<decimal>(type: "decimal(9,2)", nullable: true),
        //            Win = table.Column<bool>(type: "bit", nullable: false),
        //            FinalTeam = table.Column<short>(type: "smallint", nullable: true)
        //        },
        //        constraints: table =>
        //        {
        //            table.PrimaryKey("PK_bgd_BoardGameMatchPlayerResult", x => x.ID);
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMatchPlayerResult__bgd_BoardGameMatchPlayer",
        //                column: x => x.FK_bgd_BoardGameMatchPlayer,
        //                principalSchema: "bgd",
        //                principalTable: "BoardGameMatchPlayer",
        //                principalColumn: "ID");
        //            table.ForeignKey(
        //                name: "FK_bgd_BoardGameMatchPlayerResult__bgd_ResultType",
        //                column: x => x.FK_bgd_ResultType,
        //                principalSchema: "bgd",
        //                principalTable: "ResultType",
        //                principalColumn: "ID");
        //        });

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGame_GID",
        //        schema: "bgd",
        //        table: "BoardGame",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGame_FK_bgd_BoardGameType",
        //        schema: "bgd",
        //        table: "BoardGame",
        //        column: "FK_bgd_BoardGameType");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGame_FK_bgd_BoardGameVictoryConditionType",
        //        schema: "bgd",
        //        table: "BoardGame",
        //        column: "FK_bgd_BoardGameVictoryConditionType");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGame_FK_bgd_Publisher",
        //        schema: "bgd",
        //        table: "BoardGame",
        //        column: "FK_bgd_Publisher");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameEloMethod_GID",
        //        schema: "bgd",
        //        table: "BoardGameEloMethod",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameEloMethod_FK_bgd_BoardGame",
        //        schema: "bgd",
        //        table: "BoardGameEloMethod",
        //        column: "FK_bgd_BoardGame");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameEloMethod_FK_bgd_EloMethod",
        //        schema: "bgd",
        //        table: "BoardGameEloMethod",
        //        column: "FK_bgd_EloMethod");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameImageType_GID",
        //        schema: "bgd",
        //        table: "BoardGameImageType",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_BoardGameImageType_TypeDesc",
        //        schema: "bgd",
        //        table: "BoardGameImageType",
        //        column: "TypeDesc",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameMarker_GID",
        //        schema: "bgd",
        //        table: "BoardGameMarker",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMarker_FK_bgd_BoardGameMarkerType",
        //        schema: "bgd",
        //        table: "BoardGameMarker",
        //        column: "FK_bgd_BoardGameMarkerType");

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_BoardGameMarker_FK_bgd_BoardGame_FK_bgd_BoardGameMarkerType",
        //        schema: "bgd",
        //        table: "BoardGameMarker",
        //        columns: new[] { "FK_bgd_BoardGame", "FK_bgd_BoardGameMarkerType" },
        //        unique: true,
        //        filter: "[FK_bgd_BoardGameMarkerType] IS NOT NULL");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameMarkerType_GID",
        //        schema: "bgd",
        //        table: "BoardGameMarkerType",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMarkerType_FK_bgd_MarkerAdditionalType",
        //        schema: "bgd",
        //        table: "BoardGameMarkerType",
        //        column: "FK_bgd_MarkerAdditionalType");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMarkerType_FK_bgd_MarkerAlignmentType",
        //        schema: "bgd",
        //        table: "BoardGameMarkerType",
        //        column: "FK_bgd_MarkerAlignmentType");

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_BoardGameMarkerType_TypeDesc",
        //        schema: "bgd",
        //        table: "BoardGameMarkerType",
        //        column: "TypeDesc",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameMatch_GID",
        //        schema: "bgd",
        //        table: "BoardGameMatch",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMatch_FK_bgd_BoardGame",
        //        schema: "bgd",
        //        table: "BoardGameMatch",
        //        column: "FK_bgd_BoardGame");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMatch_FK_bgd_ResultType",
        //        schema: "bgd",
        //        table: "BoardGameMatch",
        //        column: "FK_bgd_ResultType");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameMatchPlayer_GID",
        //        schema: "bgd",
        //        table: "BoardGameMatchPlayer",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMatchPlayer_FK_bgd_BoardGameMarker",
        //        schema: "bgd",
        //        table: "BoardGameMatchPlayer",
        //        column: "FK_bgd_BoardGameMarker");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMatchPlayer_FK_bgd_BoardGameMatch",
        //        schema: "bgd",
        //        table: "BoardGameMatchPlayer",
        //        column: "FK_bgd_BoardGameMatch");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMatchPlayer_FK_bgd_Player",
        //        schema: "bgd",
        //        table: "BoardGameMatchPlayer",
        //        column: "FK_bgd_Player");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameMatchPlayerResult_GID",
        //        schema: "bgd",
        //        table: "BoardGameMatchPlayerResult",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameMatchPlayerResult_FK_bgd_ResultType",
        //        schema: "bgd",
        //        table: "BoardGameMatchPlayerResult",
        //        column: "FK_bgd_ResultType");

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_BoardGameMatchPlayerResult_FK_bgd_BoardGame_FK_bgd_ResultType",
        //        schema: "bgd",
        //        table: "BoardGameMatchPlayerResult",
        //        columns: new[] { "FK_bgd_BoardGameMatchPlayer", "FK_bgd_ResultType" },
        //        unique: true,
        //        filter: "[FK_bgd_ResultType] IS NOT NULL");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameNight_GID",
        //        schema: "bgd",
        //        table: "BoardGameNight",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameNightBoardGameMatch_GID",
        //        schema: "bgd",
        //        table: "BoardGameNightBoardGameMatch",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameNightBoardGameMatch_FK_bgd_BoardGameMatch",
        //        schema: "bgd",
        //        table: "BoardGameNightBoardGameMatch",
        //        column: "FK_bgd_BoardGameMatch");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameNightBoardGameMatch_FK_bgd_BoardGameNight",
        //        schema: "bgd",
        //        table: "BoardGameNightBoardGameMatch",
        //        column: "FK_bgd_BoardGameNight");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameNightPlayer_GID",
        //        schema: "bgd",
        //        table: "BoardGameNightPlayer",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameNightPlayer_FK_bgd_BoardGameNight",
        //        schema: "bgd",
        //        table: "BoardGameNightPlayer",
        //        column: "FK_bgd_BoardGameNight");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameNightPlayer_FK_bgd_Player",
        //        schema: "bgd",
        //        table: "BoardGameNightPlayer",
        //        column: "FK_bgd_Player");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameResult_GID",
        //        schema: "bgd",
        //        table: "BoardGameResult",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameResult_FK_bgd_ResultType",
        //        schema: "bgd",
        //        table: "BoardGameResult",
        //        column: "FK_bgd_ResultType");

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_BoardGameResult_FK_bgd_BoardGame_FK_bgd_ResultType",
        //        schema: "bgd",
        //        table: "BoardGameResult",
        //        columns: new[] { "FK_bgd_BoardGame", "FK_bgd_ResultType" },
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameShelfSection_GID",
        //        schema: "bgd",
        //        table: "BoardGameShelfSection",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_BoardGameShelfSection_FK_bgd_ShelfSection",
        //        schema: "bgd",
        //        table: "BoardGameShelfSection",
        //        column: "FK_bgd_ShelfSection");

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_BoardGameShelfSection_FK_bgd_BoardGame_FK_bgd_ShelfSection",
        //        schema: "bgd",
        //        table: "BoardGameShelfSection",
        //        columns: new[] { "FK_bgd_BoardGame", "FK_bgd_ShelfSection" },
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameType_GID",
        //        schema: "bgd",
        //        table: "BoardGameType",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_BoardGameType_TypeDesc",
        //        schema: "bgd",
        //        table: "BoardGameType",
        //        column: "TypeDesc",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_BoardGameVictoryConditionType_GID",
        //        schema: "bgd",
        //        table: "BoardGameVictoryConditionType",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_BoardGameVictoryConditionType_TypeDesc",
        //        schema: "bgd",
        //        table: "BoardGameVictoryConditionType",
        //        column: "TypeDesc",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_EloMethod_GID",
        //        schema: "bgd",
        //        table: "EloMethod",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_MarkerAdditionalType_GID",
        //        schema: "bgd",
        //        table: "MarkerAdditionalType",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_MarkerAdditionalType_TypeDesc",
        //        schema: "bgd",
        //        table: "MarkerAdditionalType",
        //        column: "TypeDesc",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_MarkerAlignmentType_GID",
        //        schema: "bgd",
        //        table: "MarkerAlignmentType",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_MarkerAlignmentType_TypeDesc",
        //        schema: "bgd",
        //        table: "MarkerAlignmentType",
        //        column: "TypeDesc",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_Player_GID",
        //        schema: "bgd",
        //        table: "Player",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_Player_FKdboAspNetUsers",
        //        schema: "bgd",
        //        table: "Player",
        //        column: "FKdboAspNetUsers");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_PlayerBoardGame_GID",
        //        schema: "bgd",
        //        table: "PlayerBoardGame",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_PlayerBoardGame_FK_bgd_BoardGame",
        //        schema: "bgd",
        //        table: "PlayerBoardGame",
        //        column: "FK_bgd_BoardGame");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_PlayerBoardGame_FK_bgd_Player_Rank",
        //        schema: "bgd",
        //        table: "PlayerBoardGame",
        //        columns: new[] { "FK_bgd_Player", "Rank" },
        //        unique: true,
        //        filter: "[FK_bgd_Player] IS NOT NULL");

        //    migrationBuilder.CreateIndex(
        //        name: "IX_PlayerBoardGame_PlayerId",
        //        schema: "bgd",
        //        table: "PlayerBoardGame",
        //        column: "PlayerId");

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_PlayerBoardGameRating_GID",
        //        schema: "bgd",
        //        table: "PlayerBoardGameRating",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_PlayerBoardGameRating_FK_bgd_BoardGame",
        //        schema: "bgd",
        //        table: "PlayerBoardGameRating",
        //        column: "FK_bgd_BoardGame");

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_PlayerBoardGameRating_FK_bgd_Player_FK_bgd_BoardGame",
        //        schema: "bgd",
        //        table: "PlayerBoardGameRating",
        //        columns: new[] { "FK_bgd_Player", "FK_bgd_BoardGame" },
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_Publisher_GID",
        //        schema: "bgd",
        //        table: "Publisher",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_RankingQueryStore_GID",
        //        schema: "bgd",
        //        table: "RankingQueryStore",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_ReleaseVersion_GID",
        //        schema: "bgd",
        //        table: "ReleaseVersion",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_ResultType_GID",
        //        schema: "bgd",
        //        table: "ResultType",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "UQ_bgd_ResultType_TypeDesc",
        //        schema: "bgd",
        //        table: "ResultType",
        //        column: "TypeDesc",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_Shelf_GID",
        //        schema: "bgd",
        //        table: "Shelf",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "AK_bgd_ShelfSection_GID",
        //        schema: "bgd",
        //        table: "ShelfSection",
        //        column: "GID",
        //        unique: true);

        //    migrationBuilder.CreateIndex(
        //        name: "IX_ShelfSection_FK_bgd_Shelf",
        //        schema: "bgd",
        //        table: "ShelfSection",
        //        column: "FK_bgd_Shelf");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        //    migrationBuilder.DropTable(
        //        name: "BoardGameEloMethod",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameImageType",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameMatchPlayerResult",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameNightBoardGameMatch",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameNightPlayer",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameResult",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameShelfSection",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "PlayerBoardGame",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "PlayerBoardGameRating",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "PlayerNameResults");

        //    migrationBuilder.DropTable(
        //        name: "RankingQueryStore",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "ReleaseVersion",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "EloMethod",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameMatchPlayer",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameNight",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "ShelfSection",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameMarker",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameMatch",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "Player",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "Shelf",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameMarkerType",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGame",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "ResultType",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "IdentityUser");

        //    migrationBuilder.DropTable(
        //        name: "MarkerAdditionalType",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "MarkerAlignmentType",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameType",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "BoardGameVictoryConditionType",
        //        schema: "bgd");

        //    migrationBuilder.DropTable(
        //        name: "Publisher",
        //        schema: "bgd");
        }
    }
}
