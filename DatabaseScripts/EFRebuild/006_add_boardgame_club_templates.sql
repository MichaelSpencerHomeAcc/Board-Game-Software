BEGIN TRANSACTION;
GO

ALTER TABLE [bgd].[BoardGame] ADD [FK_bgd_Club] bigint NULL;
GO

ALTER TABLE [bgd].[BoardGame] ADD [FK_bgd_TemplateBoardGame] bigint NULL;
GO

CREATE INDEX [IX_bgd_BoardGame_FK_bgd_Club] ON [bgd].[BoardGame] ([FK_bgd_Club]);
GO

CREATE INDEX [IX_bgd_BoardGame_FK_bgd_TemplateBoardGame] ON [bgd].[BoardGame] ([FK_bgd_TemplateBoardGame]);
GO

ALTER TABLE [bgd].[BoardGame] ADD CONSTRAINT [FK_bgd_BoardGame__bgd_Club] FOREIGN KEY ([FK_bgd_Club]) REFERENCES [bgd].[Club] ([ID]);
GO

ALTER TABLE [bgd].[BoardGame] ADD CONSTRAINT [FK_bgd_BoardGame__bgd_TemplateBoardGame] FOREIGN KEY ([FK_bgd_TemplateBoardGame]) REFERENCES [bgd].[BoardGame] ([ID]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260702201630_AddBoardGameClubTemplates', N'8.0.18');
GO

COMMIT;
GO

