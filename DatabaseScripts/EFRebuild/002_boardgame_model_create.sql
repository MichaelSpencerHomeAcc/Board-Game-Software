IF SCHEMA_ID(N'bgd') IS NULL EXEC(N'CREATE SCHEMA [bgd];');
GO


CREATE TABLE [bgd].[BoardGameImageType] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [TypeDesc] varchar(50) NOT NULL,
    [CustomSort] int NULL,
    CONSTRAINT [PK_bgd_BoardGameImageType] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameNight] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [GameNightDate] date NOT NULL,
    [Finished] bit NOT NULL,
    CONSTRAINT [PK_bgd_BoardGameNight] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameType] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [TypeDesc] varchar(50) NOT NULL,
    [CustomSort] int NULL,
    CONSTRAINT [PK_bgd_BoardGameType] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameVictoryConditionType] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [TypeDesc] varchar(50) NOT NULL,
    [CustomSort] int NULL,
    [Points] bit NULL DEFAULT CAST(0 AS bit),
    [WinLose] bit NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_bgd_BoardGameVictoryConditionType] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[EloMethod] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [MethodName] nvarchar(128) NOT NULL,
    [MethodDescription] nvarchar(255) NULL,
    [InitialMu] decimal(12,4) NOT NULL,
    [InitialSigma] decimal(12,4) NOT NULL,
    [KFactor] int NULL,
    CONSTRAINT [PK_bgd_EloMethod] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[MarkerAdditionalType] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [TypeDesc] varchar(50) NOT NULL,
    [CustomSort] int NULL,
    CONSTRAINT [PK_bgd_MarkerAdditionalType] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[MarkerAlignmentType] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [TypeDesc] varchar(50) NOT NULL,
    [CustomSort] int NULL,
    CONSTRAINT [PK_bgd_MarkerAlignmentType] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[Player] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FirstName] varchar(40) NULL,
    [MiddleName] varchar(40) NULL,
    [LastName] varchar(40) NULL,
    [DateOfBirth] date NULL,
    [FKdboAspNetUsers] varchar(450) NULL,
    CONSTRAINT [PK_bgd_Player] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[Publisher] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [PublisherName] varchar(80) NOT NULL,
    [Description] varchar(max) NULL,
    CONSTRAINT [PK_bgd_Publisher] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[RankingQueryStore] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [RankingQueryStoreName] varchar(80) NOT NULL,
    [ViewName] varchar(max) NULL,
    CONSTRAINT [PK_bgd_RankingQueryStore] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[ReleaseVersion] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [DBMajor] tinyint NULL,
    [DBMinor] tinyint NULL,
    [DBRevision] tinyint NULL,
    CONSTRAINT [PK_bgd_ReleaseVersion] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[ResultType] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [TypeDesc] varchar(50) NOT NULL,
    [IsVictory] bit NOT NULL,
    [IsDefeat] bit NOT NULL,
    [CustomSort] int NULL,
    CONSTRAINT [PK_bgd_ResultType] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[Shelf] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [ShelfName] varchar(5) NOT NULL,
    [TotalRows] tinyint NOT NULL,
    CONSTRAINT [PK_bgd_Shelf] PRIMARY KEY ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameMarkerType] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [TypeDesc] varchar(50) NOT NULL,
    [CustomSort] int NULL,
    [FK_bgd_MarkerAlignmentType] bigint NULL,
    [ImageId] nvarchar(max) NULL,
    [FK_bgd_MarkerAdditionalType] bigint NULL,
    CONSTRAINT [PK_bgd_BoardGameMarkerType] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMarkerType__bgd_MarkerAdditionalType] FOREIGN KEY ([FK_bgd_MarkerAdditionalType]) REFERENCES [bgd].[MarkerAdditionalType] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMarketType__bgd_MarkerAlignmentType] FOREIGN KEY ([FK_bgd_MarkerAlignmentType]) REFERENCES [bgd].[MarkerAlignmentType] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameNightPlayer] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGameNight] bigint NOT NULL,
    [FK_bgd_Player] bigint NOT NULL,
    CONSTRAINT [PK_bgd_BoardGameNightPlayer] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameNightPlayer__bgd_BoardGameNight] FOREIGN KEY ([FK_bgd_BoardGameNight]) REFERENCES [bgd].[BoardGameNight] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameNightPlayer__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGame] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [BoardGameName] varchar(80) NOT NULL,
    [FK_bgd_BoardGameType] bigint NULL,
    [FK_bgd_BoardGameVictoryConditionType] bigint NULL,
    [FK_bgd_Publisher] bigint NULL,
    [PlayerCountMin] tinyint NULL,
    [PlayerCountMax] tinyint NULL,
    [PlayingTimeMinInMinutes] tinyint NULL,
    [PlayingTimeMaxInMinutes] tinyint NULL,
    [ComplexityRating] decimal(9,2) NULL,
    [ReleaseDate] date NULL,
    [HasMarkers] bit NOT NULL,
    [IsExpansion] bit NOT NULL,
    [HeightCm] decimal(5,2) NULL,
    [WidthCm] decimal(5,2) NULL,
    [BoardGameSummary] varchar(max) NULL,
    [HowToPlayHyperlink] varchar(max) NULL,
    CONSTRAINT [PK_bgd_BoardGame] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGame__bgd_BoardGameType] FOREIGN KEY ([FK_bgd_BoardGameType]) REFERENCES [bgd].[BoardGameType] ([ID]),
    CONSTRAINT [FK_bgd_BoardGame__bgd_BoardGameVictoryConditionType] FOREIGN KEY ([FK_bgd_BoardGameVictoryConditionType]) REFERENCES [bgd].[BoardGameVictoryConditionType] ([ID]),
    CONSTRAINT [FK_bgd_BoardGame__bgd_Publisher] FOREIGN KEY ([FK_bgd_Publisher]) REFERENCES [bgd].[Publisher] ([ID])
);
GO


CREATE TABLE [bgd].[ShelfSection] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_Shelf] bigint NOT NULL,
    [RowNumber] tinyint NOT NULL,
    [SectionNumber] tinyint NOT NULL,
    [HeightCm] decimal(5,2) NOT NULL,
    [WidthCm] decimal(5,2) NOT NULL,
    [Blocked] bit NULL DEFAULT CAST(0 AS bit),
    [SectionName] varchar(30) NULL,
    CONSTRAINT [PK_bgd_ShelfSection] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_ShelfSection__bgd_Shelf] FOREIGN KEY ([FK_bgd_Shelf]) REFERENCES [bgd].[Shelf] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameEloMethod] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGame] bigint NOT NULL,
    [FK_bgd_EloMethod] bigint NOT NULL,
    [ExpectedWinRatioTeamA] decimal(9,2) NULL,
    [Notes] nvarchar(255) NULL,
    CONSTRAINT [PK_bgd_BoardGameEloMethod] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameEloMethod__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameEloMethod__bgd_EloMethod] FOREIGN KEY ([FK_bgd_EloMethod]) REFERENCES [bgd].[EloMethod] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameExpansion] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
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
GO


CREATE TABLE [bgd].[BoardGameMarker] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGame] bigint NOT NULL,
    [FK_bgd_BoardGameMarkerType] bigint NULL,
    CONSTRAINT [PK_bgd_BoardGameMarker] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMarker__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMarker__bgd_BoardGameMarkerType] FOREIGN KEY ([FK_bgd_BoardGameMarkerType]) REFERENCES [bgd].[BoardGameMarkerType] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameMatch] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGame] bigint NOT NULL,
    [MatchDate] datetime2 NULL,
    [FK_bgd_ResultType] bigint NULL,
    [FinishedDate] datetime2 NULL,
    [MatchComplete] bit NULL,
    CONSTRAINT [PK_bgd_BoardGameMatch] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMatch__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMatch__bgd_ResultType] FOREIGN KEY ([FK_bgd_ResultType]) REFERENCES [bgd].[ResultType] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameResult] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGame] bigint NOT NULL,
    [FK_bgd_ResultType] bigint NOT NULL,
    CONSTRAINT [PK_bgd_BoardGameResult] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameResult__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameResult__bgd_ResultType] FOREIGN KEY ([FK_bgd_ResultType]) REFERENCES [bgd].[ResultType] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameVote] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGameNight] bigint NOT NULL,
    [FK_bgd_BoardGame] bigint NOT NULL,
    [FK_bgd_Player] bigint NOT NULL,
    CONSTRAINT [PK_bgd_BoardGameVote] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameVote__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameVote__bgd_BoardGameNight] FOREIGN KEY ([FK_bgd_BoardGameNight]) REFERENCES [bgd].[BoardGameNight] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameVote__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
);
GO


CREATE TABLE [bgd].[PlayerBoardGame] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(256) NOT NULL,
    [TimeCreated] datetime2 NOT NULL,
    [ModifiedBy] nvarchar(256) NOT NULL,
    [TimeModified] datetime2 NOT NULL,
    [FK_bgd_Player] bigint NULL,
    [FK_bgd_BoardGame] bigint NULL,
    [Rank] smallint NOT NULL,
    CONSTRAINT [PK_PlayerBoardGame] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_PlayerBoardGame__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_PlayerBoardGame__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
);
GO


CREATE TABLE [bgd].[PlayerBoardGameRating] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_Player] bigint NOT NULL,
    [FK_bgd_BoardGame] bigint NOT NULL,
    [RatingMu] decimal(12,4) NOT NULL DEFAULT 25.0,
    [RatingSigma] decimal(12,4) NOT NULL DEFAULT 8.3333,
    [MatchesPlayed] int NOT NULL DEFAULT 0,
    CONSTRAINT [PK_bgd_PlayerBoardGameRating] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_PlayerBoardGameRating__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_PlayerBoardGameRating__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
);
GO


CREATE TABLE [bgd].[PlayerBoardGameStarRating] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(256) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(256) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_Player] bigint NULL,
    [FK_bgd_BoardGame] bigint NULL,
    [StarRating] decimal(9,2) NOT NULL,
    CONSTRAINT [PK_PlayerBoardGameStarRating] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_PlayerBoardGameStarRating__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_PlayerBoardGameStarRating__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameShelfSection] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGame] bigint NOT NULL,
    [FK_bgd_ShelfSection] bigint NOT NULL,
    CONSTRAINT [PK_bgd_BoardGameShelfSection] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameShelfSection__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameShelfSection__bgd_ShelfSection] FOREIGN KEY ([FK_bgd_ShelfSection]) REFERENCES [bgd].[ShelfSection] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameMatchPlayer] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGameMatch] bigint NOT NULL,
    [FK_bgd_Player] bigint NOT NULL,
    [FK_bgd_BoardGameMarker] bigint NULL,
    CONSTRAINT [PK_bgd_BoardGameMatchPlayer] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMatchPlayer__bgd_BoardGameMarker] FOREIGN KEY ([FK_bgd_BoardGameMarker]) REFERENCES [bgd].[BoardGameMarker] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMatchPlayer__bgd_BoardGameMatch] FOREIGN KEY ([FK_bgd_BoardGameMatch]) REFERENCES [bgd].[BoardGameMatch] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMatchPlayer__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameNightBoardGameMatch] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGameNight] bigint NOT NULL,
    [FK_bgd_BoardGameMatch] bigint NOT NULL,
    CONSTRAINT [PK_bgd_BoardGameNightBoardGameMatch] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameNightBoardGameMatch__bgd_BoardGameMatch] FOREIGN KEY ([FK_bgd_BoardGameMatch]) REFERENCES [bgd].[BoardGameMatch] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameNightBoardGameMatch__bgd_BoardGameNight] FOREIGN KEY ([FK_bgd_BoardGameNight]) REFERENCES [bgd].[BoardGameNight] ([ID])
);
GO


CREATE TABLE [bgd].[PlayerAchievement] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_Player] bigint NOT NULL,
    [BadgeCode] varchar(60) NOT NULL,
    [BadgeTitle] nvarchar(80) NOT NULL,
    [BadgeDetail] nvarchar(240) NOT NULL,
    [UnlockedAt] datetime NOT NULL,
    [FK_bgd_BoardGame] bigint NULL,
    [FK_bgd_BoardGameMatch] bigint NULL,
    [FK_bgd_BoardGameNight] bigint NULL,
    CONSTRAINT [PK_bgd_PlayerAchievement] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_PlayerAchievement__bgd_BoardGame] FOREIGN KEY ([FK_bgd_BoardGame]) REFERENCES [bgd].[BoardGame] ([ID]),
    CONSTRAINT [FK_bgd_PlayerAchievement__bgd_BoardGameMatch] FOREIGN KEY ([FK_bgd_BoardGameMatch]) REFERENCES [bgd].[BoardGameMatch] ([ID]),
    CONSTRAINT [FK_bgd_PlayerAchievement__bgd_BoardGameNight] FOREIGN KEY ([FK_bgd_BoardGameNight]) REFERENCES [bgd].[BoardGameNight] ([ID]),
    CONSTRAINT [FK_bgd_PlayerAchievement__bgd_Player] FOREIGN KEY ([FK_bgd_Player]) REFERENCES [bgd].[Player] ([ID])
);
GO


CREATE TABLE [bgd].[BoardGameMatchPlayerResult] (
    [ID] bigint NOT NULL IDENTITY,
    [GID] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [Inactive] bit NOT NULL,
    [VersionStamp] rowversion NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [TimeCreated] datetime NOT NULL,
    [ModifiedBy] nvarchar(128) NOT NULL,
    [TimeModified] datetime NOT NULL,
    [FK_bgd_BoardGameMatchPlayer] bigint NOT NULL,
    [FK_bgd_ResultType] bigint NULL,
    [FinalScore] decimal(9,2) NULL,
    [Win] bit NOT NULL,
    [FinalTeam] smallint NULL,
    [PreMatchRatingMu] decimal(12,4) NULL,
    [PreMatchRatingSigma] decimal(12,4) NULL,
    [RatingChangeMu] decimal(12,4) NULL,
    [RatingChangeSigma] decimal(12,4) NULL,
    CONSTRAINT [PK_bgd_BoardGameMatchPlayerResult] PRIMARY KEY ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMatchPlayerResult__bgd_BoardGameMatchPlayer] FOREIGN KEY ([FK_bgd_BoardGameMatchPlayer]) REFERENCES [bgd].[BoardGameMatchPlayer] ([ID]),
    CONSTRAINT [FK_bgd_BoardGameMatchPlayerResult__bgd_ResultType] FOREIGN KEY ([FK_bgd_ResultType]) REFERENCES [bgd].[ResultType] ([ID])
);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGame_GID] ON [bgd].[BoardGame] ([GID]);
GO


CREATE INDEX [IX_BoardGame_FK_bgd_BoardGameType] ON [bgd].[BoardGame] ([FK_bgd_BoardGameType]);
GO


CREATE INDEX [IX_BoardGame_FK_bgd_BoardGameVictoryConditionType] ON [bgd].[BoardGame] ([FK_bgd_BoardGameVictoryConditionType]);
GO


CREATE INDEX [IX_BoardGame_FK_bgd_Publisher] ON [bgd].[BoardGame] ([FK_bgd_Publisher]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameEloMethod_GID] ON [bgd].[BoardGameEloMethod] ([GID]);
GO


CREATE INDEX [IX_BoardGameEloMethod_FK_bgd_BoardGame] ON [bgd].[BoardGameEloMethod] ([FK_bgd_BoardGame]);
GO


CREATE INDEX [IX_BoardGameEloMethod_FK_bgd_EloMethod] ON [bgd].[BoardGameEloMethod] ([FK_bgd_EloMethod]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameExpansion_GID] ON [bgd].[BoardGameExpansion] ([GID]);
GO


CREATE INDEX [IX_BoardGameExpansion_FK_bgd_ExpansionBoardGame] ON [bgd].[BoardGameExpansion] ([FK_bgd_ExpansionBoardGame]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameExpansion_FK_bgd_BoardGame_FK_bgd_ExpansionBoardGame] ON [bgd].[BoardGameExpansion] ([FK_bgd_BoardGame], [FK_bgd_ExpansionBoardGame]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameImageType_GID] ON [bgd].[BoardGameImageType] ([GID]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameImageType_TypeDesc] ON [bgd].[BoardGameImageType] ([TypeDesc]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameMarker_GID] ON [bgd].[BoardGameMarker] ([GID]);
GO


CREATE INDEX [IX_BoardGameMarker_FK_bgd_BoardGameMarkerType] ON [bgd].[BoardGameMarker] ([FK_bgd_BoardGameMarkerType]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameMarker_FK_bgd_BoardGame_FK_bgd_BoardGameMarkerType] ON [bgd].[BoardGameMarker] ([FK_bgd_BoardGame], [FK_bgd_BoardGameMarkerType]) WHERE [FK_bgd_BoardGameMarkerType] IS NOT NULL;
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameMarkerType_GID] ON [bgd].[BoardGameMarkerType] ([GID]);
GO


CREATE INDEX [IX_BoardGameMarkerType_FK_bgd_MarkerAdditionalType] ON [bgd].[BoardGameMarkerType] ([FK_bgd_MarkerAdditionalType]);
GO


CREATE INDEX [IX_BoardGameMarkerType_FK_bgd_MarkerAlignmentType] ON [bgd].[BoardGameMarkerType] ([FK_bgd_MarkerAlignmentType]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameMarkerType_TypeDesc] ON [bgd].[BoardGameMarkerType] ([TypeDesc]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameMatch_GID] ON [bgd].[BoardGameMatch] ([GID]);
GO


CREATE INDEX [IX_BoardGameMatch_FK_bgd_BoardGame] ON [bgd].[BoardGameMatch] ([FK_bgd_BoardGame]);
GO


CREATE INDEX [IX_BoardGameMatch_FK_bgd_ResultType] ON [bgd].[BoardGameMatch] ([FK_bgd_ResultType]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameMatchPlayer_GID] ON [bgd].[BoardGameMatchPlayer] ([GID]);
GO


CREATE INDEX [IX_BoardGameMatchPlayer_FK_bgd_BoardGameMarker] ON [bgd].[BoardGameMatchPlayer] ([FK_bgd_BoardGameMarker]);
GO


CREATE INDEX [IX_BoardGameMatchPlayer_FK_bgd_BoardGameMatch] ON [bgd].[BoardGameMatchPlayer] ([FK_bgd_BoardGameMatch]);
GO


CREATE INDEX [IX_BoardGameMatchPlayer_FK_bgd_Player] ON [bgd].[BoardGameMatchPlayer] ([FK_bgd_Player]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameMatchPlayerResult_GID] ON [bgd].[BoardGameMatchPlayerResult] ([GID]);
GO


CREATE INDEX [IX_BoardGameMatchPlayerResult_FK_bgd_ResultType] ON [bgd].[BoardGameMatchPlayerResult] ([FK_bgd_ResultType]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameMatchPlayerResult_FK_bgd_BoardGame_FK_bgd_ResultType] ON [bgd].[BoardGameMatchPlayerResult] ([FK_bgd_BoardGameMatchPlayer], [FK_bgd_ResultType]) WHERE [FK_bgd_ResultType] IS NOT NULL;
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameNight_GID] ON [bgd].[BoardGameNight] ([GID]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameNightBoardGameMatch_GID] ON [bgd].[BoardGameNightBoardGameMatch] ([GID]);
GO


CREATE INDEX [IX_BoardGameNightBoardGameMatch_FK_bgd_BoardGameMatch] ON [bgd].[BoardGameNightBoardGameMatch] ([FK_bgd_BoardGameMatch]);
GO


CREATE INDEX [IX_BoardGameNightBoardGameMatch_FK_bgd_BoardGameNight] ON [bgd].[BoardGameNightBoardGameMatch] ([FK_bgd_BoardGameNight]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameNightPlayer_GID] ON [bgd].[BoardGameNightPlayer] ([GID]);
GO


CREATE INDEX [IX_BoardGameNightPlayer_FK_bgd_BoardGameNight] ON [bgd].[BoardGameNightPlayer] ([FK_bgd_BoardGameNight]);
GO


CREATE INDEX [IX_BoardGameNightPlayer_FK_bgd_Player] ON [bgd].[BoardGameNightPlayer] ([FK_bgd_Player]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameResult_GID] ON [bgd].[BoardGameResult] ([GID]);
GO


CREATE INDEX [IX_BoardGameResult_FK_bgd_ResultType] ON [bgd].[BoardGameResult] ([FK_bgd_ResultType]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameResult_FK_bgd_BoardGame_FK_bgd_ResultType] ON [bgd].[BoardGameResult] ([FK_bgd_BoardGame], [FK_bgd_ResultType]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameShelfSection_GID] ON [bgd].[BoardGameShelfSection] ([GID]);
GO


CREATE INDEX [IX_BoardGameShelfSection_FK_bgd_ShelfSection] ON [bgd].[BoardGameShelfSection] ([FK_bgd_ShelfSection]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameShelfSection_FK_bgd_BoardGame_FK_bgd_ShelfSection] ON [bgd].[BoardGameShelfSection] ([FK_bgd_BoardGame], [FK_bgd_ShelfSection]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameType_GID] ON [bgd].[BoardGameType] ([GID]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameType_TypeDesc] ON [bgd].[BoardGameType] ([TypeDesc]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameVictoryConditionType_GID] ON [bgd].[BoardGameVictoryConditionType] ([GID]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameVictoryConditionType_TypeDesc] ON [bgd].[BoardGameVictoryConditionType] ([TypeDesc]);
GO


CREATE UNIQUE INDEX [AK_bgd_BoardGameVote_GID] ON [bgd].[BoardGameVote] ([GID]);
GO


CREATE INDEX [IX_BoardGameVote_FK_bgd_BoardGame] ON [bgd].[BoardGameVote] ([FK_bgd_BoardGame]);
GO


CREATE INDEX [IX_BoardGameVote_FK_bgd_Player] ON [bgd].[BoardGameVote] ([FK_bgd_Player]);
GO


CREATE UNIQUE INDEX [UQ_bgd_BoardGameVote_Night_Game_Player] ON [bgd].[BoardGameVote] ([FK_bgd_BoardGameNight], [FK_bgd_BoardGame], [FK_bgd_Player]);
GO


CREATE UNIQUE INDEX [AK_bgd_EloMethod_GID] ON [bgd].[EloMethod] ([GID]);
GO


CREATE UNIQUE INDEX [AK_bgd_MarkerAdditionalType_GID] ON [bgd].[MarkerAdditionalType] ([GID]);
GO


CREATE UNIQUE INDEX [UQ_bgd_MarkerAdditionalType_TypeDesc] ON [bgd].[MarkerAdditionalType] ([TypeDesc]);
GO


CREATE UNIQUE INDEX [AK_bgd_MarkerAlignmentType_GID] ON [bgd].[MarkerAlignmentType] ([GID]);
GO


CREATE UNIQUE INDEX [UQ_bgd_MarkerAlignmentType_TypeDesc] ON [bgd].[MarkerAlignmentType] ([TypeDesc]);
GO


CREATE UNIQUE INDEX [AK_bgd_Player_GID] ON [bgd].[Player] ([GID]);
GO


CREATE UNIQUE INDEX [AK_bgd_PlayerAchievement_GID] ON [bgd].[PlayerAchievement] ([GID]);
GO


CREATE INDEX [IX_PlayerAchievement_FK_bgd_BoardGame] ON [bgd].[PlayerAchievement] ([FK_bgd_BoardGame]);
GO


CREATE INDEX [IX_PlayerAchievement_FK_bgd_BoardGameMatch] ON [bgd].[PlayerAchievement] ([FK_bgd_BoardGameMatch]);
GO


CREATE INDEX [IX_PlayerAchievement_FK_bgd_BoardGameNight] ON [bgd].[PlayerAchievement] ([FK_bgd_BoardGameNight]);
GO


CREATE UNIQUE INDEX [UQ_bgd_PlayerAchievement_Scope] ON [bgd].[PlayerAchievement] ([FK_bgd_Player], [BadgeCode], [FK_bgd_BoardGame], [FK_bgd_BoardGameMatch], [FK_bgd_BoardGameNight]) WHERE [FK_bgd_BoardGame] IS NOT NULL AND [FK_bgd_BoardGameMatch] IS NOT NULL AND [FK_bgd_BoardGameNight] IS NOT NULL;
GO


CREATE UNIQUE INDEX [AK_bgd_PlayerBoardGame_GID] ON [bgd].[PlayerBoardGame] ([GID]);
GO


CREATE INDEX [IX_PlayerBoardGame_FK_bgd_BoardGame] ON [bgd].[PlayerBoardGame] ([FK_bgd_BoardGame]);
GO


CREATE UNIQUE INDEX [IX_PlayerBoardGame_FK_bgd_Player_Rank] ON [bgd].[PlayerBoardGame] ([FK_bgd_Player], [Rank]) WHERE [FK_bgd_Player] IS NOT NULL AND [Inactive] = 0;
GO


CREATE UNIQUE INDEX [AK_bgd_PlayerBoardGameRating_GID] ON [bgd].[PlayerBoardGameRating] ([GID]);
GO


CREATE INDEX [IX_PlayerBoardGameRating_FK_bgd_BoardGame] ON [bgd].[PlayerBoardGameRating] ([FK_bgd_BoardGame]);
GO


CREATE UNIQUE INDEX [UQ_bgd_PlayerBoardGameRating_FK_bgd_Player_FK_bgd_BoardGame] ON [bgd].[PlayerBoardGameRating] ([FK_bgd_Player], [FK_bgd_BoardGame]);
GO


CREATE UNIQUE INDEX [AK_bgd_PlayerBoardGameStarRating_GID] ON [bgd].[PlayerBoardGameStarRating] ([GID]);
GO


CREATE INDEX [IX_PlayerBoardGameStarRating_FK_bgd_BoardGame] ON [bgd].[PlayerBoardGameStarRating] ([FK_bgd_BoardGame]);
GO


CREATE INDEX [IX_PlayerBoardGameStarRating_FK_bgd_Player] ON [bgd].[PlayerBoardGameStarRating] ([FK_bgd_Player]);
GO


CREATE UNIQUE INDEX [AK_bgd_Publisher_GID] ON [bgd].[Publisher] ([GID]);
GO


CREATE UNIQUE INDEX [AK_bgd_RankingQueryStore_GID] ON [bgd].[RankingQueryStore] ([GID]);
GO


CREATE UNIQUE INDEX [AK_bgd_ReleaseVersion_GID] ON [bgd].[ReleaseVersion] ([GID]);
GO


CREATE UNIQUE INDEX [AK_bgd_ResultType_GID] ON [bgd].[ResultType] ([GID]);
GO


CREATE UNIQUE INDEX [UQ_bgd_ResultType_TypeDesc] ON [bgd].[ResultType] ([TypeDesc]);
GO


CREATE UNIQUE INDEX [AK_bgd_Shelf_GID] ON [bgd].[Shelf] ([GID]);
GO


CREATE UNIQUE INDEX [AK_bgd_ShelfSection_GID] ON [bgd].[ShelfSection] ([GID]);
GO


CREATE INDEX [IX_ShelfSection_FK_bgd_Shelf] ON [bgd].[ShelfSection] ([FK_bgd_Shelf]);
GO


