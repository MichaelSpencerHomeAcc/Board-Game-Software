using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Board_Game_Software.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardGameExpansions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'bgd.BoardGame', N'IsExpansion') IS NULL
                BEGIN
                    ALTER TABLE [bgd].[BoardGame] ADD [IsExpansion] bit NOT NULL CONSTRAINT [DF_bgd_BoardGame_IsExpansion] DEFAULT CAST(0 AS bit);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'bgd.BoardGameExpansion', N'U') IS NULL
                BEGIN
                    CREATE TABLE [bgd].[BoardGameExpansion] (
                        [ID] bigint NOT NULL IDENTITY,
                        [GID] uniqueidentifier NOT NULL CONSTRAINT [DF_bgd_BoardGameExpansion_GID] DEFAULT (newid()),
                        [Inactive] bit NOT NULL,
                        [VersionStamp] rowversion NULL,
                        [CreatedBy] nvarchar(128) NOT NULL,
                        [TimeCreated] datetime NOT NULL,
                        [ModifiedBy] nvarchar(128) NOT NULL,
                        [TimeModified] datetime NOT NULL,
                        [FK_bgd_BoardGame] bigint NOT NULL,
                        [FK_bgd_ExpansionBoardGame] bigint NOT NULL,
                        CONSTRAINT [PK_bgd_BoardGameExpansion] PRIMARY KEY ([ID]),
                        CONSTRAINT [FK_bgd_BoardGameExpansion__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
                        CONSTRAINT [FK_bgd_BoardGameExpansion__bgd_ExpansionBoardGame] FOREIGN KEY ([FK_bgd_ExpansionBoardGame]) REFERENCES [bgd].[BoardGame] ([ID])
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'AK_bgd_BoardGameExpansion_GID' AND [object_id] = OBJECT_ID(N'bgd.BoardGameExpansion'))
                    CREATE UNIQUE INDEX [AK_bgd_BoardGameExpansion_GID] ON [bgd].[BoardGameExpansion] ([GID]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_BoardGameExpansion_FK_bgd_ExpansionBoardGame' AND [object_id] = OBJECT_ID(N'bgd.BoardGameExpansion'))
                    CREATE INDEX [IX_BoardGameExpansion_FK_bgd_ExpansionBoardGame] ON [bgd].[BoardGameExpansion] ([FK_bgd_ExpansionBoardGame]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UQ_bgd_BoardGameExpansion_FK_bgd_BoardGame_FK_bgd_ExpansionBoardGame' AND [object_id] = OBJECT_ID(N'bgd.BoardGameExpansion'))
                    CREATE UNIQUE INDEX [UQ_bgd_BoardGameExpansion_FK_bgd_BoardGame_FK_bgd_ExpansionBoardGame] ON [bgd].[BoardGameExpansion] ([FK_bgd_BoardGame], [FK_bgd_ExpansionBoardGame]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardGameExpansion",
                schema: "bgd");

            migrationBuilder.DropColumn(
                name: "IsExpansion",
                schema: "bgd",
                table: "BoardGame");
        }
    }
}
