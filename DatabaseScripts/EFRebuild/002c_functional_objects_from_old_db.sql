/* Extracted from tironicus_BoardGames. Functional views/functions only; audit triggers intentionally excluded. */
/* Creation order handles dependencies between functions and views. */
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'[bgd].[vwShelfSection]', N'V') IS NOT NULL DROP VIEW [bgd].[vwShelfSection];
GO
IF OBJECT_ID(N'[bgd].[vwShelfLocationView]', N'V') IS NOT NULL DROP VIEW [bgd].[vwShelfLocationView];
GO
IF OBJECT_ID(N'[bgd].[vwShelf]', N'V') IS NOT NULL DROP VIEW [bgd].[vwShelf];
GO
IF OBJECT_ID(N'[bgd].[vwResultType]', N'V') IS NOT NULL DROP VIEW [bgd].[vwResultType];
GO
IF OBJECT_ID(N'[bgd].[vwRankingQueryStore]', N'V') IS NOT NULL DROP VIEW [bgd].[vwRankingQueryStore];
GO
IF OBJECT_ID(N'[bgd].[vwPublisher]', N'V') IS NOT NULL DROP VIEW [bgd].[vwPublisher];
GO
IF OBJECT_ID(N'[bgd].[vwPlayerBoardGameRating]', N'V') IS NOT NULL DROP VIEW [bgd].[vwPlayerBoardGameRating];
GO
IF OBJECT_ID(N'[bgd].[vwPlayer]', N'V') IS NOT NULL DROP VIEW [bgd].[vwPlayer];
GO
IF OBJECT_ID(N'[bgd].[vwMarkerAlignmentType]', N'V') IS NOT NULL DROP VIEW [bgd].[vwMarkerAlignmentType];
GO
IF OBJECT_ID(N'[bgd].[vwGameNightPlayerRankings]', N'V') IS NOT NULL DROP VIEW [bgd].[vwGameNightPlayerRankings];
GO
IF OBJECT_ID(N'[bgd].[vwGameNightPlayerPoints]', N'V') IS NOT NULL DROP VIEW [bgd].[vwGameNightPlayerPoints];
GO
IF OBJECT_ID(N'[bgd].[vwGameHistory]', N'V') IS NOT NULL DROP VIEW [bgd].[vwGameHistory];
GO
IF OBJECT_ID(N'[bgd].[VwEloRanking]', N'V') IS NOT NULL DROP VIEW [bgd].[VwEloRanking];
GO
IF OBJECT_ID(N'[bgd].[vwEloMethod]', N'V') IS NOT NULL DROP VIEW [bgd].[vwEloMethod];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameVictoryConditionType]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameVictoryConditionType];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameType]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameType];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameShelfSection]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameShelfSection];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameResult]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameResult];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameNightPlayer]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameNightPlayer];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameNightBoardGameMatch]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameNightBoardGameMatch];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameNight]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameNight];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameMatchPlayerResult]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameMatchPlayerResult];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameMatchPlayer]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameMatchPlayer];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameMatch]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameMatch];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameMarkerType]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameMarkerType];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameMarker]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameMarker];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameImageType]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameImageType];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGameEloMethod]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGameEloMethod];
GO
IF OBJECT_ID(N'[bgd].[vwBoardGame]', N'V') IS NOT NULL DROP VIEW [bgd].[vwBoardGame];
GO
IF OBJECT_ID(N'[bgd].[GetMatchWinners]') IS NOT NULL DROP FUNCTION [bgd].[GetMatchWinners];
GO
IF OBJECT_ID(N'[bgd].[GetMostPlayedMarker]') IS NOT NULL DROP FUNCTION [bgd].[GetMostPlayedMarker];
GO
IF OBJECT_ID(N'[bgd].[GetOrdinalPosition]') IS NOT NULL DROP FUNCTION [bgd].[GetOrdinalPosition];
GO
IF OBJECT_ID(N'[bgd].[GetPlayerAlignmentStats]') IS NOT NULL DROP FUNCTION [bgd].[GetPlayerAlignmentStats];
GO
IF OBJECT_ID(N'[bgd].[GetPlayerWinsForGame]') IS NOT NULL DROP FUNCTION [bgd].[GetPlayerWinsForGame];
GO
IF OBJECT_ID(N'[bgd].[GetShelfLocation]') IS NOT NULL DROP FUNCTION [bgd].[GetShelfLocation];
GO
IF OBJECT_ID(N'[bgd].[MostCommonWinner]') IS NOT NULL DROP FUNCTION [bgd].[MostCommonWinner];
GO
IF OBJECT_ID(N'[bgd].[ReturnPlayerName]') IS NOT NULL DROP FUNCTION [bgd].[ReturnPlayerName];
GO
IF OBJECT_ID(N'[bgd].[TotalPlayCount]') IS NOT NULL DROP FUNCTION [bgd].[TotalPlayCount];
GO
/* Foundation functions used by other functions. */
CREATE function [bgd].[ReturnPlayerName] (
	@ID bigint
)
returns table
as
return
	select
			rtrim(coalesce(p.firstname + ' ', '') + coalesce(nullif(p.middlename + ' ', ' '), '') + coalesce(p.lastname + ' ', '')) as FullName
		from
			bgd.Player as p
		where
			p.ID = @ID
GO
/* Base functions. */
CREATE FUNCTION bgd.GetMatchWinners(@BoardGameMatchID BIGINT)
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @WinnerList NVARCHAR(MAX);

    WITH RankedPlayers AS (
        SELECT 
            bgp.FK_bgd_BoardGameMatch AS MatchID,
            f.FullName AS PlayerName,
            bgpr.FinalScore,
            bgpr.Win,
            RANK() OVER (PARTITION BY bgp.FK_bgd_BoardGameMatch ORDER BY bgpr.FinalScore DESC) AS ScoreRank
        FROM bgd.BoardGameMatchPlayer AS bgp
        LEFT JOIN bgd.BoardGameMatchPlayerResult AS bgpr 
            ON bgpr.FK_bgd_BoardGameMatchPlayer = bgp.ID
        JOIN bgd.Player p ON p.ID = bgp.FK_bgd_Player
		OUTER APPLY bgd.ReturnPlayerName (
			p.ID
		) AS f
        WHERE bgp.FK_bgd_BoardGameMatch = @BoardGameMatchID
    )
    SELECT @WinnerList = 
        STUFF(
            (SELECT ', ' + PlayerName
             FROM RankedPlayers
             WHERE 
                (FinalScore IS NOT NULL AND ScoreRank = 1)  -- Get top scorer(s)
                OR (FinalScore IS NULL AND Win = 1)         -- Get winners in win/lose mode
             FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)')
        , 1, 2, '');

    RETURN @WinnerList;
END;

GO
CREATE FUNCTION [bgd].[GetMostPlayedMarker]
(@BoardGameId BIGINT, @PlayerId BIGINT)
RETURNS NVARCHAR (100)
AS
BEGIN
    DECLARE @MarkerName AS NVARCHAR (100);
    SELECT   TOP 1 @MarkerName = mt.TypeDesc
    FROM     [bgd].[BoardGameMatchPlayerResult] AS r
             INNER JOIN
             [bgd].[BoardGameMatchPlayer] AS m
             ON r.[Fk_Bgd_BoardGameMatchPlayer] = m.[Id]
             INNER JOIN
             [bgd].[BoardGameMatch] AS mm
             ON m.[Fk_Bgd_BoardGameMatch] = mm.[Id]
             INNER JOIN
             [bgd].[BoardGameMarker] AS bgm
             ON m.[FK_Bgd_BoardGameMarker] = bgm.[Id]
             INNER JOIN
             [bgd].[BoardGameMarkerType] AS mt
             ON bgm.[Fk_Bgd_BoardGameMarkerType] = mt.[Id]
    WHERE    mm.[Fk_Bgd_BoardGame] = @BoardGameId
             AND m.[Fk_Bgd_Player] = @PlayerId
             AND mm.MatchComplete = 1
             AND r.Inactive = 0
    GROUP BY mt.TypeDesc
    ORDER BY COUNT(*) DESC;
    RETURN ISNULL(@MarkerName, 'None');
END


GO
CREATE FUNCTION bgd.GetOrdinalPosition (@Rank INT)
RETURNS NVARCHAR(10)
AS
BEGIN
    DECLARE @Suffix NVARCHAR(2)

    IF @Rank % 100 BETWEEN 11 AND 13
        SET @Suffix = 'th'
    ELSE
        SET @Suffix = 
            CASE @Rank % 10
                WHEN 1 THEN 'st'
                WHEN 2 THEN 'nd'
                WHEN 3 THEN 'rd'
                ELSE 'th'
            END

    RETURN CAST(@Rank AS NVARCHAR(10)) + @Suffix
END

GO
CREATE FUNCTION [bgd].[GetPlayerAlignmentStats]
(@BoardGameId BIGINT, @PlayerId BIGINT)
RETURNS NVARCHAR (MAX)
AS
BEGIN
    DECLARE @Result AS NVARCHAR (MAX);
    SELECT @Result = STRING_AGG(CAST (AlignmentData AS NVARCHAR (MAX)), ' | ')
    FROM   (SELECT   mat.TypeDesc + ': ' + CAST (COUNT(r.Id) AS NVARCHAR (10)) AS AlignmentData
            FROM     [bgd].[BoardGameMatchPlayerResult] AS r
                     INNER JOIN
                     [bgd].[BoardGameMatchPlayer] AS m
                     ON r.[Fk_Bgd_BoardGameMatchPlayer] = m.[Id]
                     INNER JOIN
                     [bgd].[BoardGameMatch] AS mm
                     ON m.[Fk_Bgd_BoardGameMatch] = mm.[Id]
                     INNER JOIN
                     [bgd].[BoardGameMarker] AS bgm
                     ON m.[FK_Bgd_BoardGameMarker] = bgm.[Id]
                     INNER JOIN
                     [bgd].[BoardGameMarkerType] AS mt
                     ON bgm.[Fk_Bgd_BoardGameMarkerType] = mt.[Id]
                     INNER JOIN
                     [bgd].[MarkerAlignmentType] AS mat
                     ON mt.[Fk_Bgd_MarkerAlignmentType] = mat.[Id]
            WHERE    mm.[Fk_Bgd_BoardGame] = @BoardGameId
                     AND m.[Fk_Bgd_Player] = @PlayerId
                     AND r.Win = 1
                     AND mm.MatchComplete = 1
                     AND r.Inactive = 0
            GROUP BY mat.TypeDesc) AS Src;
    RETURN ISNULL(@Result, 'N/A');
END


GO
CREATE FUNCTION [bgd].[GetPlayerWinsForGame]
(@BoardGameId BIGINT, @PlayerId BIGINT)
RETURNS INT
AS
BEGIN
    DECLARE @WinCount AS INT;
    SELECT @WinCount = COUNT(r.Id)
    FROM   [bgd].[BoardGameMatchPlayerResult] AS r
           INNER JOIN
           [bgd].[BoardGameMatchPlayer] AS m
           ON r.[Fk_Bgd_BoardGameMatchPlayer] = m.[Id]
           INNER JOIN
           bgd.BoardGameMatch AS mm
           ON m.[Fk_Bgd_BoardGameMatch] = mm.[Id]
    WHERE  mm.[Fk_Bgd_BoardGame] = @BoardGameId
           AND m.[Fk_Bgd_Player] = @PlayerId
           AND r.Win = 1
           AND mm.MatchComplete = 1;
    RETURN ISNULL(@WinCount, 0);
END


GO

CREATE FUNCTION bgd.MostCommonWinner (	
	@BoardGameID BIGINT
)
RETURNS TABLE 
AS
RETURN 
(
		WITH Stats_Base AS (
	SELECT	
			bgm.ID
			, bgmpr.FinalScore
			, bgmpr.Win
			, fn.FullName
			, CASE WHEN bgmpr.FinalScore IS NOT NULL 
					THEN RANK() OVER (PARTITION BY bgm.ID ORDER BY bgmpr.FinalScore DESC)
				ELSE
					RANK() OVER (PARTITION BY bgm.ID ORDER BY bgmpr.Win DESC)
			END AS Ranking
			, bg.BoardGameName
		FROM
			bgd.BoardGameMatch AS bgm
				LEFT JOIN bgd.BoardGameMatchPlayer AS bgmp
					ON bgm.ID = bgmp.FK_bgd_BoardGameMatch
				LEFT JOIN bgd.BoardGameMatchPlayerResult AS bgmpr
					ON bgmp.ID = bgmpr.FK_bgd_BoardGameMatchPlayer
				OUTER APPLY bgd.ReturnPlayerName (
					bgmp.FK_bgd_Player
				) as fn
				LEFT JOIN bgd.BoardGame AS bg
					ON bgm.FK_bgd_BoardGame = bg.ID
		WHERE
			bg.ID = @BoardGameID
	) 
	, VictoryCount AS (
		SELECT
				FullName
				, COUNT(ID) AS Victories
			FROM
				Stats_Base
			WHERE
				Ranking = 1
			GROUP BY
				FullName
	) 
	, MaxVictories AS (
		SELECT 
				MAX(Victories) AS MaxVictoryCount 
			FROM 
				VictoryCount
	)

	SELECT 
		STUFF((
			SELECT 
					', ' + FullName
				FROM 
					VictoryCount
				WHERE 
					Victories = (
									SELECT 
											MaxVictoryCount 
										FROM 
											MaxVictories
				)
				 FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS TopPlayers
)

GO
/* Views required by delayed functions. */

/*
	0007 Amend bgd.vwBoardGameMatch
	Author: Michael Spencer
*/

-- Create View
CREATE view [bgd].[vwBoardGameMatch]
as
select
			bgm.ID
			, bgm.GID
			, bgm.Inactive
			, bgm.VersionStamp
			, bgm.CreatedBy
			, bgm.TimeCreated
			, bgm.ModifiedBy
			, bgm.TimeModified
			, bgm.FK_bgd_BoardGame
			, bgm.MatchDate
			, bgm.FK_bgd_ResultType
			, bgm.FinishedDate
			, bg.BoardGameName
			, rt.TypeDesc as ResultType_TypeDesc
			, bgd.GetMatchWinners(bgm.ID) as Winner
		from bgd.BoardGameMatch as bgm
			left outer join bgd.BoardGame as bg
				on bgm.FK_bgd_BoardGame = bg.ID
			left outer join bgd.ResultType as rt
				on bgm.FK_bgd_ResultType = rt.ID
;

GO

-- Create View
create view bgd.vwBoardGameShelfSection
as
select
			bgpr.ID
			, bgpr.GID
			, bgpr.Inactive
			, bgpr.VersionStamp
			, bgpr.CreatedBy
			, bgpr.TimeCreated
			, bgpr.ModifiedBy
			, bgpr.TimeModified
			, bgpr.FK_bgd_BoardGame
			, bgpr.FK_bgd_ShelfSection
			, bg.BoardGameName
			, s.ShelfName + CAST(ss.RowNumber AS NVARCHAR) AS LocationCode
		from bgd.BoardGameShelfSection as bgpr
			left outer join bgd.ShelfSection as ss
				on bgpr.FK_bgd_ShelfSection = ss.ID
			left outer join bgd.BoardGame as bg
				on bgpr.FK_bgd_BoardGame = bg.ID
			left outer join bgd.Shelf as s
				on ss.FK_bgd_Shelf = s.ID
;

GO
/* Functions that depend on prerequisite views. */
create function bgd.GetShelfLocation (
	@ID as bigint
)
RETURNS TABLE
AS
RETURN
	SELECT
			vbgss.LocationCode
		FROM
			bgd.vwBoardGameShelfSection as vbgss
		WHERE
			vbgss.FK_bgd_BoardGame = @ID

GO
/*
	0003 bgd.TotalPlayCount
	Author: Michael Spencer
*/

create function bgd.TotalPlayCount (
	@ID as bigint
)
RETURNS TABLE
AS
RETURN
	SELECT
			count(vbgm.ID) as PlayedCount
		FROM
			bgd.vwBoardGameMatch as vbgm
		WHERE
			vbgm.FK_bgd_BoardGame = @ID

GO
/* Remaining views. */
CREATE VIEW bgd.vwGameNightPlayerPoints AS
WITH RankedPlayers AS (
    SELECT 
        bgm.FK_bgd_BoardGame AS BoardGameID,
        bgm.ID AS MatchID,
        bgnbm.FK_bgd_BoardGameNight AS GameNightID,
        bgmp.FK_bgd_Player AS PlayerID,
        COALESCE(bgpr.FinalScore, 0) AS PlayerScore,
        bgpr.Win AS IsWinner,
        COUNT(bgmp.FK_bgd_Player) OVER (PARTITION BY bgm.ID) AS TotalPlayers,
        RANK() OVER (
            PARTITION BY bgm.ID 
            ORDER BY COALESCE(bgpr.FinalScore, 0) DESC
        ) AS RankPosition,
        CASE 
            WHEN bgpr.FinalScore IS NULL THEN 1
            ELSE 0
        END AS IsWinLoseGame
    FROM bgd.BoardGameMatch bgm
    JOIN bgd.BoardGameNightBoardGameMatch bgnbm ON bgm.ID = bgnbm.FK_bgd_BoardGameMatch
    JOIN bgd.BoardGameMatchPlayer bgmp ON bgm.ID = bgmp.FK_bgd_BoardGameMatch
    LEFT JOIN bgd.BoardGameMatchPlayerResult bgpr ON bgmp.ID = bgpr.FK_bgd_BoardGameMatchPlayer
)
SELECT 
    GameNightID,
    MatchID,
    BoardGameID,
    PlayerID,
    PlayerScore,
    IsWinner,
    TotalPlayers,
    CASE 
        WHEN IsWinLoseGame = 1 AND IsWinner = 1 THEN 5
        WHEN IsWinLoseGame = 1 AND IsWinner = 0 THEN 2
        ELSE TotalPlayers - RankPosition + 1
    END AS PointsAwarded
FROM RankedPlayers;

GO



CREATE VIEW [bgd].[vwGameNightPlayerRankings] AS
WITH GameNightPoints AS (
    SELECT 
        GameNightID,
        PlayerID,
        SUM(PointsAwarded) AS TotalPoints
    FROM bgd.vwGameNightPlayerPoints
    GROUP BY GameNightID, PlayerID
)
SELECT 
    gnp.GameNightID,
    gnp.PlayerID,
	f.FullName AS PlayerName,
    gnp.TotalPoints,
	RANK() OVER (PARTITION BY gnp.GameNightID ORDER BY gnp.TotalPoints DESC) as Ranking,
    bgd.GetOrdinalPosition(RANK() OVER (PARTITION BY gnp.GameNightID ORDER BY gnp.TotalPoints DESC)) AS OverallRank
FROM GameNightPoints gnp
JOIN bgd.Player p ON gnp.PlayerID = p.ID
OUTER APPLY bgd.ReturnPlayerName (
	p.ID
) AS f

GO

-- Create View
CREATE view [bgd].[vwBoardGame]
as
select
			bg.ID
			, bg.GID
			, bg.Inactive
			, bg.VersionStamp
			, bg.CreatedBy
			, bg.TimeCreated
			, bg.ModifiedBy
			, bg.TimeModified
			, bg.BoardGameName
			, bg.FK_bgd_BoardGameType
			, bg.FK_bgd_BoardGameVictoryConditionType
			, bg.FK_bgd_Publisher
			, bg.PlayerCountMin
			, bg.PlayerCountMax
			, bg.PlayingTimeMinInMinutes
			, bg.PlayingTimeMaxInMinutes
			, bg.ComplexityRating
			, bg.ReleaseDate
			, bg.HasMarkers
			, bg.HeightCm
			, bg.WidthCm
			, bg.BoardGameSummary
			, bg.HowToPlayHyperlink
			, f.LocationCode
			, f2.PlayedCount
			, bgt.TypeDesc as BoardGameType
		from bgd.BoardGame as bg
			outer apply bgd.GetShelfLocation (
				bg.ID
			) as f
			outer apply bgd.TotalPlayCount (
				bg.ID
			) as f2
			left join bgd.BoardGameType as bgt
				on bg.FK_bgd_BoardGameType = bgt.ID
;

GO

-- Create View
create view bgd.vwBoardGameEloMethod
as
select
			bg.ID
			, bg.GID
			, bg.Inactive
			, bg.VersionStamp
			, bg.CreatedBy
			, bg.TimeCreated
			, bg.ModifiedBy
			, bg.TimeModified
			, bg.FK_bgd_BoardGame
			, bg.FK_bgd_EloMethod
			, bg.ExpectedWinRatioTeamA
			, bg.Notes
		from bgd.BoardGameEloMethod as bg
;

GO

-- Create View
create view bgd.vwBoardGameImageType
as
select
			bgt.ID
			, bgt.GID
			, bgt.Inactive
			, bgt.VersionStamp
			, bgt.CreatedBy
			, bgt.TimeCreated
			, bgt.ModifiedBy
			, bgt.TimeModified
			, bgt.TypeDesc
			, bgt.CustomSort
		from bgd.BoardGameImageType as bgt
;

GO
create view bgd.vwBoardGameMarker
as
select
			bgpr.ID
			, bgpr.GID
			, bgpr.Inactive
			, bgpr.VersionStamp
			, bgpr.CreatedBy
			, bgpr.TimeCreated
			, bgpr.ModifiedBy
			, bgpr.TimeModified
			, bgpr.FK_bgd_BoardGame
			, bgpr.FK_bgd_BoardGameMarkerType
			, bg.BoardGameName
			, bgmt.TypeDesc as BoardGameMarkerType_TypeDesc
		from bgd.BoardGameMarker as bgpr
			left outer join bgd.BoardGameMarkerType as bgmt
				on bgpr.FK_bgd_BoardGameMarkerType = bgmt.ID
			left outer join bgd.BoardGame as bg
				on bgpr.FK_bgd_BoardGame = bg.ID
;

GO

-- Create View
create view bgd.vwBoardGameMarkerType
as
select
			bgt.ID
			, bgt.GID
			, bgt.Inactive
			, bgt.VersionStamp
			, bgt.CreatedBy
			, bgt.TimeCreated
			, bgt.ModifiedBy
			, bgt.TimeModified
			, bgt.TypeDesc
			, bgt.CustomSort
		from bgd.BoardGameMarkerType as bgt
;

GO
/*
	0005 Amend bgd.vwBoardGameMatchPlayer
	Author: Michael Spencer
*/

-- Create View
CREATE view [bgd].[vwBoardGameMatchPlayer]
as
select
			bgmp.ID
			, bgmp.GID
			, bgmp.Inactive
			, bgmp.VersionStamp
			, bgmp.CreatedBy
			, bgmp.TimeCreated
			, bgmp.ModifiedBy
			, bgmp.TimeModified
			, bgmp.FK_bgd_BoardGameMatch
			, bgmp.FK_bgd_Player
			, bgmp.FK_bgd_BoardGameMarker
			, bg.BoardGameName
			, bgm.MatchDate
			, rpn.FullName
			, bgmt.TypeDesc as BoardGameMarkerType_TypeDesc
		from bgd.BoardGameMatchPlayer as bgmp
			left outer join bgd.BoardGameMatch as bgm
				on bgmp.FK_bgd_BoardGameMatch = bgm.ID
			left outer join bgd.BoardGame as bg
				on bgm.FK_bgd_BoardGame = bg.ID
			left outer join bgd.Player as p
				on bgmp.FK_bgd_Player = p.ID
			outer apply bgd.ReturnPlayerName(p.ID) as rpn
			left outer join bgd.BoardGameMarker as bgmr
				on bgmp.FK_bgd_BoardGameMarker = bgmr.ID
			left outer join bgd.BoardGameMarkerType as bgmt
				on bgmr.FK_bgd_BoardGameMarkerType = bgmt.ID
;

GO

-- alter View
CREATE view bgd.vwBoardGameMatchPlayerResult
as
select
			bgmpr.ID
			, bgmpr.GID
			, bgmpr.Inactive
			, bgmpr.VersionStamp
			, bgmpr.CreatedBy
			, bgmpr.TimeCreated
			, bgmpr.ModifiedBy
			, bgmpr.TimeModified
			, bgmpr.FK_bgd_BoardGameMatchPlayer
			, bgmpr.FK_bgd_ResultType
			, bgmpr.FinalScore
			, bgmpr.Win
			, bg.BoardGameName
			, rt.TypeDesc as ResultType_TypeDesc
			, rt.IsVictory
			, rt.IsDefeat
		from bgd.BoardGameMatchPlayerResult as bgmpr
			left outer join bgd.ResultType as rt
				on bgmpr.FK_bgd_ResultType = rt.ID
			left outer join bgd.BoardGameMatchPlayer as bgmp
				on bgmpr.FK_bgd_BoardGameMatchPlayer = bgmp.ID
			left outer join bgd.BoardGameMatch as bgm
				on bgmp.FK_bgd_BoardGameMatch = bgm.ID
			left outer join bgd.BoardGame as bg
				on bgm.FK_bgd_BoardGame = bg.ID
;

GO
CREATE VIEW bgd.[VwBoardGameNight]
AS
SELECT
    n.[Id],
    n.[Gid],
    n.[Inactive],
    n.[VersionStamp],
    n.[CreatedBy],
    n.[TimeCreated],
    n.[ModifiedBy],
    n.[TimeModified],
    n.[GameNightDate],
    n.[Finished],

    -- Active players on this night
    (SELECT COUNT(1)
     FROM [bgd].[BoardGameNightPlayer] AS p
     WHERE p.[Fk_Bgd_BoardGameNight] = n.[Id]
       AND p.[Inactive] = 0) AS [PlayerCount],

    -- Active matches linked to this night
    (SELECT COUNT(1)
     FROM [bgd].[BoardGameNightBoardGameMatch] AS m
     WHERE m.[Fk_Bgd_BoardGameNight] = n.[Id]
       AND m.[Inactive] = 0) AS [MatchCount]
FROM [bgd].[BoardGameNight] AS n;


GO

-- Create View
create view bgd.vwBoardGameNightBoardGameMatch
as
select
			bg.ID
			, bg.GID
			, bg.Inactive
			, bg.VersionStamp
			, bg.CreatedBy
			, bg.TimeCreated
			, bg.ModifiedBy
			, bg.TimeModified
			, bg.FK_bgd_BoardGameNight
			, bg.FK_bgd_BoardGameMatch
		from bgd.BoardGameNightBoardGameMatch as bg
;

GO

-- Create View
create view bgd.vwBoardGameNightPlayer
as
select
			bg.ID
			, bg.GID
			, bg.Inactive
			, bg.VersionStamp
			, bg.CreatedBy
			, bg.TimeCreated
			, bg.ModifiedBy
			, bg.TimeModified
			, bg.FK_bgd_BoardGameNight
			, bg.FK_bgd_Player
		from bgd.BoardGameNightPlayer as bg
;

GO

-- Create View
create view bgd.vwBoardGameResult
as
select
			bgpr.ID
			, bgpr.GID
			, bgpr.Inactive
			, bgpr.VersionStamp
			, bgpr.CreatedBy
			, bgpr.TimeCreated
			, bgpr.ModifiedBy
			, bgpr.TimeModified
			, bgpr.FK_bgd_BoardGame
			, bgpr.FK_bgd_ResultType
			, bg.BoardGameName
			, rt.TypeDesc as ResultType_TypeDesc
			, rt.IsVictory
			, rt.IsDefeat
		from bgd.BoardGameResult as bgpr
			left outer join bgd.ResultType as rt
				on bgpr.FK_bgd_ResultType = rt.ID
			left outer join bgd.BoardGame as bg
				on bgpr.FK_bgd_BoardGame = bg.ID
;

GO

-- Create View
create view bgd.vwBoardGameType
as
select
			bgt.ID
			, bgt.GID
			, bgt.Inactive
			, bgt.VersionStamp
			, bgt.CreatedBy
			, bgt.TimeCreated
			, bgt.ModifiedBy
			, bgt.TimeModified
			, bgt.TypeDesc
			, bgt.CustomSort
		from bgd.BoardGameType as bgt
;

GO

-- alter View
CREATE view bgd.vwBoardGameVictoryConditionType
as
select
			bgvct.ID
			, bgvct.GID
			, bgvct.Inactive
			, bgvct.VersionStamp
			, bgvct.CreatedBy
			, bgvct.TimeCreated
			, bgvct.ModifiedBy
			, bgvct.TimeModified
			, bgvct.TypeDesc
			, bgvct.CustomSort
			, bgvct.Points
			, bgvct.WinLose
		from bgd.BoardGameVictoryConditionType as bgvct
;

GO

-- Create View
create view bgd.vwEloMethod
as
select
			bg.ID
			, bg.GID
			, bg.Inactive
			, bg.VersionStamp
			, bg.CreatedBy
			, bg.TimeCreated
			, bg.ModifiedBy
			, bg.TimeModified
			, bg.MethodName
			, bg.MethodDescription
		from bgd.EloMethod as bg
;

GO
CREATE VIEW [bgd].[VwEloRanking]
AS
SELECT r.[Id],
       r.[Gid],
       r.[Inactive],
       r.[VersionStamp],
       r.[CreatedBy],
       r.[TimeCreated],
       r.[ModifiedBy],
       r.[TimeModified],
       r.[Fk_Bgd_BoardGame],
       bg.[BoardGameName],
       r.[Fk_Bgd_Player],
       p.[FirstName] AS [PlayerFirstName],
       p.[LastName] AS [PlayerLastName],
       ft.FullName AS [PlayerFullName],
       r.[RatingMu],
       r.[RatingSigma],
       r.[MatchesPlayed],
       [bgd].[GetPlayerWinsForGame](r.[Fk_Bgd_BoardGame], r.[Fk_Bgd_Player]) AS [TotalWins],
       [bgd].[GetPlayerAlignmentStats](r.[Fk_Bgd_BoardGame], r.[Fk_Bgd_Player]) AS [AlignmentWins],
       [bgd].[GetMostPlayedMarker](r.[Fk_Bgd_BoardGame], r.[Fk_Bgd_Player]) AS [MostPlayedToken],
       (SELECT TOP 1 mat.TypeDesc
        FROM   [bgd].[BoardGameMarkerType] AS mt
               INNER JOIN
               [bgd].MarkerAlignmentType AS mat
               ON mt.fk_bgd_markeralignmenttype = mat.id
        WHERE  mt.TypeDesc = [bgd].[GetMostPlayedMarker](r.[Fk_Bgd_BoardGame], r.[Fk_Bgd_Player])) AS [MainTokenAlignment],
       CAST (ROUND(1500 + (50 * (r.[RatingMu] - (3 * r.[RatingSigma]))), 0) AS DECIMAL (18, 0)) AS [DisplayRating],
       RANK() OVER (PARTITION BY r.[Fk_Bgd_BoardGame] ORDER BY ROUND(1500 + (50 * (r.[RatingMu] - (3 * r.[RatingSigma]))), 0) DESC, [bgd].[GetPlayerWinsForGame](r.[Fk_Bgd_BoardGame], r.[Fk_Bgd_Player]) DESC) AS [CalculatedRank]
FROM   [bgd].[PlayerBoardGameRating] AS r
       INNER JOIN
       [bgd].[BoardGame] AS bg
       ON r.[Fk_Bgd_BoardGame] = bg.[Id]
       INNER JOIN
       [bgd].[Player] AS p
       ON r.[Fk_Bgd_Player] = p.[Id] CROSS APPLY [bgd].ReturnPlayerName(r.Fk_Bgd_Player) AS ft;


GO

-- Create View
create view bgd.vwMarkerAlignmentType
as
select
			bgt.ID
			, bgt.GID
			, bgt.Inactive
			, bgt.VersionStamp
			, bgt.CreatedBy
			, bgt.TimeCreated
			, bgt.ModifiedBy
			, bgt.TimeModified
			, bgt.TypeDesc
			, bgt.CustomSort
		from bgd.MarkerAlignmentType as bgt
;

GO

/*
	0001 Amend vwPlayer
	Author: Michael Spencer
*/

CREATE view [bgd].[vwPlayer]
as
	select
			p.ID
			, p.GID
			, p.Inactive
			, p.VersionStamp
			, p.CreatedBy
			, p.TimeCreated
			, p.ModifiedBy
			, p.TimeModified
			, p.FirstName
			, p.MiddleName
			, p.LastName
			, p.DateOfBirth
			, p.FKdboAspNetUsers
			, rpn.FullName
		from bgd.Player as p
			outer apply bgd.ReturnPlayerName(p.ID) as rpn
;

GO
CREATE   VIEW [bgd].[vwPlayerBoardGameRating]
AS
SELECT pbgr.ID,
       pbgr.GID,
       pbgr.FK_bgd_Player,
       pbgr.FK_bgd_BoardGame,
       pbgr.RatingMu,
       pbgr.RatingSigma,
       pbgr.MatchesPlayed,
       CAST (1500 + (ISNULL(pbgr.RatingMu, 25.0) * 40) - (ISNULL(pbgr.RatingSigma, 8.3333) * 120) AS INT) AS MatchScore,
       CAST (ISNULL(pbgr.RatingMu, 25.0) - (3 * ISNULL(pbgr.RatingSigma, 8.3333)) AS DECIMAL (12, 4)) AS DisplayRating,
       bg.BoardGameName,
       ft.FullName
FROM   bgd.PlayerBoardGameRating AS pbgr
       LEFT OUTER JOIN
       bgd.BoardGame AS bg
       ON pbgr.FK_bgd_BoardGame = bg.ID
       LEFT OUTER JOIN
       bgd.Player AS p
       ON pbgr.FK_bgd_Player = p.ID CROSS APPLY bgd.ReturnPlayerName(p.ID) AS ft;


GO

-- Create View
create view bgd.vwPublisher
as
select
			p.ID
			, p.GID
			, p.Inactive
			, p.VersionStamp
			, p.CreatedBy
			, p.TimeCreated
			, p.ModifiedBy
			, p.TimeModified
			, p.PublisherName
			, p.Description
		from bgd.Publisher as p
;

GO

-- Create View
create view bgd.vwRankingQueryStore
as
select
			p.ID
			, p.GID
			, p.Inactive
			, p.VersionStamp
			, p.CreatedBy
			, p.TimeCreated
			, p.ModifiedBy
			, p.TimeModified
			, p.RankingQueryStoreName
			, p.ViewName
		from bgd.RankingQueryStore as p
;

GO

-- Create View
create view bgd.vwResultType
as
select
			prt.ID
			, prt.GID
			, prt.Inactive
			, prt.VersionStamp
			, prt.CreatedBy
			, prt.TimeCreated
			, prt.ModifiedBy
			, prt.TimeModified
			, prt.TypeDesc
			, prt.IsVictory
			, prt.IsDefeat
			, prt.CustomSort
		from bgd.ResultType as prt
;

GO

-- Create View
create view bgd.vwShelf
as
select
			s.ID
			, s.GID
			, s.Inactive
			, s.VersionStamp
			, s.CreatedBy
			, s.TimeCreated
			, s.ModifiedBy
			, s.TimeModified
			, s.ShelfName
			, s.TotalRows
		from bgd.Shelf as s
;

GO

CREATE View [bgd].[vwShelfLocationView] 
as
SELECT 
    ss.ID,
	ss.GID,
	s.ShelfName + CAST(ss.RowNumber AS NVARCHAR) + '-' + CAST(ss.SectionNumber AS NVARCHAR) AS Location,
    s.ShelfName,
    ss.RowNumber,
    ss.SectionNumber,
    ss.WidthCm,
    ss.Blocked,
	ss.SectionName
FROM bgd.ShelfSection ss
JOIN bgd.Shelf s ON ss.FK_bgd_Shelf = s.ID


GO


-- Create View
CREATE view [bgd].[vwShelfSection]
as
select
			ss.ID
			, ss.GID
			, ss.Inactive
			, ss.VersionStamp
			, ss.CreatedBy
			, ss.TimeCreated
			, ss.ModifiedBy
			, ss.TimeModified
			, ss.FK_bgd_Shelf
			, ss.RowNumber
			, ss.SectionNumber
			, ss.HeightCm
			, ss.WidthCm
			, ss.Blocked
			, ss.SectionName
		from bgd.ShelfSection as ss
;

GO

CREATE VIEW [bgd].[vwGameHistory] AS
SELECT
		bg.BoardGameName
		, COUNT(bgm.ID) AS PlayedCount
		, mcw.TopPlayers
	FROM
		bgd.BoardGameMatch AS bgm
			LEFT JOIN bgd.BoardGame AS bg
				ON bgm.FK_bgd_BoardGame = bg.ID
			OUTER APPLY bgd.MostCommonWinner (
				bg.ID
			) AS mcw
	GROUP BY
		bg.BoardGameName
		, mcw.TopPlayers

GO
