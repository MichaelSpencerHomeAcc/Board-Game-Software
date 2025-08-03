using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Board_Game_Software.Models;

public partial class BoardGameDbContext : DbContext
{
    public BoardGameDbContext()
    {
    }

    public BoardGameDbContext(DbContextOptions<BoardGameDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BoardGame> BoardGames { get; set; }

    public virtual DbSet<BoardGameEloMethod> BoardGameEloMethods { get; set; }

    public virtual DbSet<BoardGameImageType> BoardGameImageTypes { get; set; }

    public virtual DbSet<BoardGameMarker> BoardGameMarkers { get; set; }

    public virtual DbSet<BoardGameMarkerType> BoardGameMarkerTypes { get; set; }

    public virtual DbSet<BoardGameMatch> BoardGameMatches { get; set; }

    public virtual DbSet<BoardGameMatchPlayer> BoardGameMatchPlayers { get; set; }

    public virtual DbSet<BoardGameMatchPlayerResult> BoardGameMatchPlayerResults { get; set; }

    public virtual DbSet<BoardGameNight> BoardGameNights { get; set; }

    public virtual DbSet<BoardGameNightBoardGameMatch> BoardGameNightBoardGameMatches { get; set; }

    public virtual DbSet<BoardGameNightPlayer> BoardGameNightPlayers { get; set; }

    public virtual DbSet<BoardGameResult> BoardGameResults { get; set; }

    public virtual DbSet<BoardGameShelfSection> BoardGameShelfSections { get; set; }

    public virtual DbSet<BoardGameType> BoardGameTypes { get; set; }

    public virtual DbSet<BoardGameVictoryConditionType> BoardGameVictoryConditionTypes { get; set; }

    public virtual DbSet<EloMethod> EloMethods { get; set; }

    public virtual DbSet<MarkerAlignmentType> MarkerAlignmentTypes { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerBoardGameRating> PlayerBoardGameRatings { get; set; }

    public virtual DbSet<Publisher> Publishers { get; set; }

    public virtual DbSet<RankingQueryStore> RankingQueryStores { get; set; }

    public virtual DbSet<ReleaseVersion> ReleaseVersions { get; set; }

    public virtual DbSet<ResultType> ResultTypes { get; set; }

    public virtual DbSet<Shelf> Shelves { get; set; }

    public virtual DbSet<ShelfSection> ShelfSections { get; set; }

    public virtual DbSet<MarkerAdditionalType> MarkerAdditionalTypes { get; set; }

    public virtual DbSet<VwBoardGame> VwBoardGames { get; set; }

    public virtual DbSet<VwBoardGameEloMethod> VwBoardGameEloMethods { get; set; }

    public virtual DbSet<VwBoardGameImageType> VwBoardGameImageTypes { get; set; }

    public virtual DbSet<VwBoardGameMarker> VwBoardGameMarkers { get; set; }

    public virtual DbSet<VwBoardGameMarkerType> VwBoardGameMarkerTypes { get; set; }

    public virtual DbSet<VwBoardGameMatch> VwBoardGameMatches { get; set; }

    public virtual DbSet<VwBoardGameMatchPlayer> VwBoardGameMatchPlayers { get; set; }

    public virtual DbSet<VwBoardGameMatchPlayerResult> VwBoardGameMatchPlayerResults { get; set; }

    public virtual DbSet<VwBoardGameNight> VwBoardGameNights { get; set; }

    public virtual DbSet<VwBoardGameNightBoardGameMatch> VwBoardGameNightBoardGameMatches { get; set; }

    public virtual DbSet<VwBoardGameNightPlayer> VwBoardGameNightPlayers { get; set; }

    public virtual DbSet<VwBoardGameResult> VwBoardGameResults { get; set; }

    public virtual DbSet<VwBoardGameShelfSection> VwBoardGameShelfSections { get; set; }

    public virtual DbSet<VwBoardGameType> VwBoardGameTypes { get; set; }

    public virtual DbSet<VwBoardGameVictoryConditionType> VwBoardGameVictoryConditionTypes { get; set; }

    public virtual DbSet<VwEloMethod> VwEloMethods { get; set; }

    public virtual DbSet<VwGameHistory> VwGameHistories { get; set; }

    public virtual DbSet<VwGameNightPlayerPoint> VwGameNightPlayerPoints { get; set; }

    public virtual DbSet<VwGameNightPlayerRanking> VwGameNightPlayerRankings { get; set; }

    public virtual DbSet<VwMarkerAlignmentType> VwMarkerAlignmentTypes { get; set; }

    public virtual DbSet<VwPlayer> VwPlayers { get; set; }

    public virtual DbSet<VwPlayerBoardGameRating> VwPlayerBoardGameRatings { get; set; }

    public virtual DbSet<VwPublisher> VwPublishers { get; set; }

    public virtual DbSet<VwRankingQueryStore> VwRankingQueryStores { get; set; }

    public virtual DbSet<VwResultType> VwResultTypes { get; set; }

    public virtual DbSet<VwShelf> VwShelves { get; set; }

    public virtual DbSet<VwShelfLocationView> VwShelfLocationViews { get; set; }

    public virtual DbSet<VwShelfSection> VwShelfSections { get; set; }

    public virtual DbSet<PlayerNameResult> PlayerNameResults { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=sql.bsite.net\\MSSQL2016;Database=tironicus_BoardGames;User Id=tironicus_BoardGames;Password=Crash454!;TrustServerCertificate=True;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoardGame>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGame");

            entity.ToTable("BoardGame", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGame_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGame_InsertAfter");
                    tb.HasTrigger("bgdBoardGame_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGame_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.BoardGameSummary).IsUnicode(false);
            entity.Property(e => e.ComplexityRating).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGameType).HasColumnName("FK_bgd_BoardGameType");
            entity.Property(e => e.FkBgdBoardGameVictoryConditionType).HasColumnName("FK_bgd_BoardGameVictoryConditionType");
            entity.Property(e => e.FkBgdPublisher).HasColumnName("FK_bgd_Publisher");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.HeightCm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.HowToPlayHyperlink).IsUnicode(false);
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.WidthCm).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.FkBgdBoardGameTypeNavigation).WithMany(p => p.BoardGames)
                .HasForeignKey(d => d.FkBgdBoardGameType)
                .HasConstraintName("FK_bgd_BoardGame__bgd_BoardGameType");

            entity.HasOne(d => d.FkBgdBoardGameVictoryConditionTypeNavigation).WithMany(p => p.BoardGames)
                .HasForeignKey(d => d.FkBgdBoardGameVictoryConditionType)
                .HasConstraintName("FK_bgd_BoardGame__bgd_BoardGameVictoryConditionType");

            entity.HasOne(d => d.FkBgdPublisherNavigation).WithMany(p => p.BoardGames)
                .HasForeignKey(d => d.FkBgdPublisher)
                .HasConstraintName("FK_bgd_BoardGame__bgd_Publisher");
        });

        modelBuilder.Entity<BoardGameEloMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameEloMethod");

            entity.ToTable("BoardGameEloMethod", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameEloMethod_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameEloMethod_InsertAfter");
                    tb.HasTrigger("bgdBoardGameEloMethod_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameEloMethod_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.ExpectedWinRatioTeamA).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdEloMethod).HasColumnName("FK_bgd_EloMethod");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameNavigation).WithMany(p => p.BoardGameEloMethods)
                .HasForeignKey(d => d.FkBgdBoardGame)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameEloMethod__bgd_BoardGame");

            entity.HasOne(d => d.FkBgdEloMethodNavigation).WithMany(p => p.BoardGameEloMethods)
                .HasForeignKey(d => d.FkBgdEloMethod)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameEloMethod__bgd_EloMethod");
        });

        modelBuilder.Entity<BoardGameImageType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameImageType");

            entity.ToTable("BoardGameImageType", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameImageType_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameImageType_InsertAfter");
                    tb.HasTrigger("bgdBoardGameImageType_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameImageType_GID").IsUnique();

            entity.HasIndex(e => e.TypeDesc, "UQ_bgd_BoardGameImageType_TypeDesc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<BoardGameMarker>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameMarker");

            entity.ToTable("BoardGameMarker", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameMarker_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameMarker_InsertAfter");
                    tb.HasTrigger("bgdBoardGameMarker_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameMarker_GID").IsUnique();

            entity.HasIndex(e => new { e.FkBgdBoardGame, e.FkBgdBoardGameMarkerType }, "UQ_bgd_BoardGameMarker_FK_bgd_BoardGame_FK_bgd_BoardGameMarkerType").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdBoardGameMarkerType).HasColumnName("FK_bgd_BoardGameMarkerType");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameNavigation).WithMany(p => p.BoardGameMarkers)
                .HasForeignKey(d => d.FkBgdBoardGame)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameMarker__bgd_BoardGame");

            entity.HasOne(d => d.FkBgdBoardGameMarkerTypeNavigation).WithMany(p => p.BoardGameMarkers)
                .HasForeignKey(d => d.FkBgdBoardGameMarkerType)
                .HasConstraintName("FK_bgd_BoardGameMarker__bgd_BoardGameMarkerType");
        });

        modelBuilder.Entity<BoardGameMarkerType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameMarkerType");

            entity.ToTable("BoardGameMarkerType", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameMarkerType_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameMarkerType_InsertAfter");
                    tb.HasTrigger("bgdBoardGameMarkerType_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameMarkerType_GID").IsUnique();

            entity.HasIndex(e => e.TypeDesc, "UQ_bgd_BoardGameMarkerType_TypeDesc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdMarkerAlignmentType).HasColumnName("FK_bgd_MarkerAlignmentType");
            entity.Property(e => e.FkBgdMarkerAdditionalType).HasColumnName("FK_bgd_MarkerAdditionalType");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdMarkerAlignmentTypeNavigation).WithMany(p => p.BoardGameMarkerTypes)
                .HasForeignKey(d => d.FkBgdMarkerAlignmentType)
                .HasConstraintName("FK_bgd_BoardGameMarketType__bgd_MarkerAlignmentType");
            entity.HasOne(d => d.FkBgdMarkerAdditionalTypeNavigation)
                .WithMany(p => p.BoardGameMarkerTypes)
                .HasForeignKey(d => d.FkBgdMarkerAdditionalType)
                .HasConstraintName("FK_bgd_BoardGameMarkerType__bgd_MarkerAdditionalType");

        });

        modelBuilder.Entity<BoardGameMatch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameMatch");

            entity.ToTable("BoardGameMatch", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameMatch_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameMatch_InsertAfter");
                    tb.HasTrigger("bgdBoardGameMatch_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameMatch_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdResultType).HasColumnName("FK_bgd_ResultType");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameNavigation).WithMany(p => p.BoardGameMatches)
                .HasForeignKey(d => d.FkBgdBoardGame)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameMatch__bgd_BoardGame");

            entity.HasOne(d => d.FkBgdResultTypeNavigation).WithMany(p => p.BoardGameMatches)
                .HasForeignKey(d => d.FkBgdResultType)
                .HasConstraintName("FK_bgd_BoardGameMatch__bgd_ResultType");
        });

        modelBuilder.Entity<BoardGameMatchPlayer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameMatchPlayer");

            entity.ToTable("BoardGameMatchPlayer", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameMatchPlayer_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameMatchPlayer_InsertAfter");
                    tb.HasTrigger("bgdBoardGameMatchPlayer_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameMatchPlayer_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGameMarker).HasColumnName("FK_bgd_BoardGameMarker");
            entity.Property(e => e.FkBgdBoardGameMatch).HasColumnName("FK_bgd_BoardGameMatch");
            entity.Property(e => e.FkBgdPlayer).HasColumnName("FK_bgd_Player");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameMarkerNavigation).WithMany(p => p.BoardGameMatchPlayers)
                .HasForeignKey(d => d.FkBgdBoardGameMarker)
                .HasConstraintName("FK_bgd_BoardGameMatchPlayer__bgd_BoardGameMarker");

            entity.HasOne(d => d.FkBgdBoardGameMatchNavigation).WithMany(p => p.BoardGameMatchPlayers)
                .HasForeignKey(d => d.FkBgdBoardGameMatch)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameMatchPlayer__bgd_BoardGameMatch");

            entity.HasOne(d => d.FkBgdPlayerNavigation).WithMany(p => p.BoardGameMatchPlayers)
                .HasForeignKey(d => d.FkBgdPlayer)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameMatchPlayer__bgd_Player");
        });

        modelBuilder.Entity<BoardGameMatchPlayerResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameMatchPlayerResult");

            entity.ToTable("BoardGameMatchPlayerResult", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameMatchPlayerResult_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameMatchPlayerResult_InsertAfter");
                    tb.HasTrigger("bgdBoardGameMatchPlayerResult_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameMatchPlayerResult_GID").IsUnique();

            entity.HasIndex(e => new { e.FkBgdBoardGameMatchPlayer, e.FkBgdResultType }, "UQ_bgd_BoardGameMatchPlayerResult_FK_bgd_BoardGame_FK_bgd_ResultType").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FinalScore).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.FkBgdBoardGameMatchPlayer).HasColumnName("FK_bgd_BoardGameMatchPlayer");
            entity.Property(e => e.FkBgdResultType).HasColumnName("FK_bgd_ResultType");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameMatchPlayerNavigation).WithMany(p => p.BoardGameMatchPlayerResults)
                .HasForeignKey(d => d.FkBgdBoardGameMatchPlayer)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameMatchPlayerResult__bgd_BoardGameMatchPlayer");

            entity.HasOne(d => d.FkBgdResultTypeNavigation).WithMany(p => p.BoardGameMatchPlayerResults)
                .HasForeignKey(d => d.FkBgdResultType)
                .HasConstraintName("FK_bgd_BoardGameMatchPlayerResult__bgd_ResultType");
        });

        modelBuilder.Entity<BoardGameNight>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameNight");

            entity.ToTable("BoardGameNight", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameNight_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameNight_InsertAfter");
                    tb.HasTrigger("bgdBoardGameNight_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameNight_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<BoardGameNightBoardGameMatch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameNightBoardGameMatch");

            entity.ToTable("BoardGameNightBoardGameMatch", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameNightBoardGameMatch_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameNightBoardGameMatch_InsertAfter");
                    tb.HasTrigger("bgdBoardGameNightBoardGameMatch_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameNightBoardGameMatch_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGameMatch).HasColumnName("FK_bgd_BoardGameMatch");
            entity.Property(e => e.FkBgdBoardGameNight).HasColumnName("FK_bgd_BoardGameNight");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameMatchNavigation).WithMany(p => p.BoardGameNightBoardGameMatches)
                .HasForeignKey(d => d.FkBgdBoardGameMatch)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameNightBoardGameMatch__bgd_BoardGameMatch");

            entity.HasOne(d => d.FkBgdBoardGameNightNavigation).WithMany(p => p.BoardGameNightBoardGameMatches)
                .HasForeignKey(d => d.FkBgdBoardGameNight)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameNightBoardGameMatch__bgd_BoardGameNight");
        });

        modelBuilder.Entity<BoardGameNightPlayer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameNightPlayer");

            entity.ToTable("BoardGameNightPlayer", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameNightPlayer_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameNightPlayer_InsertAfter");
                    tb.HasTrigger("bgdBoardGameNightPlayer_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameNightPlayer_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGameNight).HasColumnName("FK_bgd_BoardGameNight");
            entity.Property(e => e.FkBgdPlayer).HasColumnName("FK_bgd_Player");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameNightNavigation).WithMany(p => p.BoardGameNightPlayers)
                .HasForeignKey(d => d.FkBgdBoardGameNight)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameNightPlayer__bgd_BoardGameNight");

            entity.HasOne(d => d.FkBgdPlayerNavigation).WithMany(p => p.BoardGameNightPlayers)
                .HasForeignKey(d => d.FkBgdPlayer)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameNightPlayer__bgd_Player");
        });

        modelBuilder.Entity<BoardGameResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameResult");

            entity.ToTable("BoardGameResult", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameResult_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameResult_InsertAfter");
                    tb.HasTrigger("bgdBoardGameResult_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameResult_GID").IsUnique();

            entity.HasIndex(e => new { e.FkBgdBoardGame, e.FkBgdResultType }, "UQ_bgd_BoardGameResult_FK_bgd_BoardGame_FK_bgd_ResultType").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdResultType).HasColumnName("FK_bgd_ResultType");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameNavigation).WithMany(p => p.BoardGameResults)
                .HasForeignKey(d => d.FkBgdBoardGame)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameResult__bgd_BoardGame");

            entity.HasOne(d => d.FkBgdResultTypeNavigation).WithMany(p => p.BoardGameResults)
                .HasForeignKey(d => d.FkBgdResultType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameResult__bgd_ResultType");
        });

        modelBuilder.Entity<BoardGameShelfSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameShelfSection");

            entity.ToTable("BoardGameShelfSection", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameShelfSection_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameShelfSection_InsertAfter");
                    tb.HasTrigger("bgdBoardGameShelfSection_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameShelfSection_GID").IsUnique();

            entity.HasIndex(e => new { e.FkBgdBoardGame, e.FkBgdShelfSection }, "UQ_bgd_BoardGameShelfSection_FK_bgd_BoardGame_FK_bgd_ShelfSection").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdShelfSection).HasColumnName("FK_bgd_ShelfSection");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameNavigation).WithMany(p => p.BoardGameShelfSections)
                .HasForeignKey(d => d.FkBgdBoardGame)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameShelfSection__bgd_BoardGame");

            entity.HasOne(d => d.FkBgdShelfSectionNavigation).WithMany(p => p.BoardGameShelfSections)
                .HasForeignKey(d => d.FkBgdShelfSection)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_BoardGameShelfSection__bgd_ShelfSection");
        });

        modelBuilder.Entity<BoardGameType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameType");

            entity.ToTable("BoardGameType", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameType_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameType_InsertAfter");
                    tb.HasTrigger("bgdBoardGameType_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameType_GID").IsUnique();

            entity.HasIndex(e => e.TypeDesc, "UQ_bgd_BoardGameType_TypeDesc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<BoardGameVictoryConditionType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_BoardGameVictoryConditionType");

            entity.ToTable("BoardGameVictoryConditionType", "bgd", tb =>
                {
                    tb.HasTrigger("bgdBoardGameVictoryConditionType_DeleteInsteadOf");
                    tb.HasTrigger("bgdBoardGameVictoryConditionType_InsertAfter");
                    tb.HasTrigger("bgdBoardGameVictoryConditionType_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_BoardGameVictoryConditionType_GID").IsUnique();

            entity.HasIndex(e => e.TypeDesc, "UQ_bgd_BoardGameVictoryConditionType_TypeDesc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.Points).HasDefaultValue(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.WinLose).HasDefaultValue(false);
        });

        modelBuilder.Entity<EloMethod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_EloMethod");

            entity.ToTable("EloMethod", "bgd", tb =>
                {
                    tb.HasTrigger("bgdEloMethod_DeleteInsteadOf");
                    tb.HasTrigger("bgdEloMethod_InsertAfter");
                    tb.HasTrigger("bgdEloMethod_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_EloMethod_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.MethodDescription).HasMaxLength(255);
            entity.Property(e => e.MethodName).HasMaxLength(128);
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<MarkerAlignmentType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_MarkerAlignmentType");

            entity.ToTable("MarkerAlignmentType", "bgd", tb =>
                {
                    tb.HasTrigger("bgdMarkerAlignmentType_DeleteInsteadOf");
                    tb.HasTrigger("bgdMarkerAlignmentType_InsertAfter");
                    tb.HasTrigger("bgdMarkerAlignmentType_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_MarkerAlignmentType_GID").IsUnique();

            entity.HasIndex(e => e.TypeDesc, "UQ_bgd_MarkerAlignmentType_TypeDesc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_Player");

            entity.ToTable("Player", "bgd", tb =>
                {
                    tb.HasTrigger("bgdPlayer_DeleteInsteadOf");
                    tb.HasTrigger("bgdPlayer_InsertAfter");
                    tb.HasTrigger("bgdPlayer_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_Player_GID").IsUnique();
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.FkdboAspNetUsers)
                .HasConstraintName("FK_Player_AspNetUsers");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkdboAspNetUsers)
                .HasColumnName("FKdboAspNetUsers")
                .HasMaxLength(450)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.LastName)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.MiddleName)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<PlayerBoardGameRating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_PlayerBoardGameRating");

            entity.ToTable("PlayerBoardGameRating", "bgd", tb =>
                {
                    tb.HasTrigger("bgdPlayerBoardGameRating_DeleteInsteadOf");
                    tb.HasTrigger("bgdPlayerBoardGameRating_InsertAfter");
                    tb.HasTrigger("bgdPlayerBoardGameRating_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_PlayerBoardGameRating_GID").IsUnique();

            entity.HasIndex(e => new { e.FkBgdPlayer, e.FkBgdBoardGame }, "UQ_bgd_PlayerBoardGameRating_FK_bgd_Player_FK_bgd_BoardGame").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdPlayer).HasColumnName("FK_bgd_Player");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.Rating).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.FkBgdBoardGameNavigation).WithMany(p => p.PlayerBoardGameRatings)
                .HasForeignKey(d => d.FkBgdBoardGame)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_PlayerBoardGameRating__bgd_BoardGame");

            entity.HasOne(d => d.FkBgdPlayerNavigation).WithMany(p => p.PlayerBoardGameRatings)
                .HasForeignKey(d => d.FkBgdPlayer)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_PlayerBoardGameRating__bgd_Player");
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_Publisher");

            entity.ToTable("Publisher", "bgd", tb =>
                {
                    tb.HasTrigger("bgdPublisher_DeleteInsteadOf");
                    tb.HasTrigger("bgdPublisher_InsertAfter");
                    tb.HasTrigger("bgdPublisher_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_Publisher_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.PublisherName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<RankingQueryStore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_RankingQueryStore");

            entity.ToTable("RankingQueryStore", "bgd", tb =>
                {
                    tb.HasTrigger("bgdRankingQueryStore_DeleteInsteadOf");
                    tb.HasTrigger("bgdRankingQueryStore_InsertAfter");
                    tb.HasTrigger("bgdRankingQueryStore_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_RankingQueryStore_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.RankingQueryStoreName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.ViewName).IsUnicode(false);
        });

        modelBuilder.Entity<ReleaseVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_ReleaseVersion");

            entity.ToTable("ReleaseVersion", "bgd");

            entity.HasIndex(e => e.Gid, "AK_bgd_ReleaseVersion_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Dbmajor).HasColumnName("DBMajor");
            entity.Property(e => e.Dbminor).HasColumnName("DBMinor");
            entity.Property(e => e.Dbrevision).HasColumnName("DBRevision");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<ResultType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_ResultType");

            entity.ToTable("ResultType", "bgd", tb =>
                {
                    tb.HasTrigger("bgdResultType_DeleteInsteadOf");
                    tb.HasTrigger("bgdResultType_InsertAfter");
                    tb.HasTrigger("bgdResultType_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_ResultType_GID").IsUnique();

            entity.HasIndex(e => e.TypeDesc, "UQ_bgd_ResultType_TypeDesc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_Shelf");

            entity.ToTable("Shelf", "bgd", tb =>
                {
                    tb.HasTrigger("bgdShelf_DeleteInsteadOf");
                    tb.HasTrigger("bgdShelf_InsertAfter");
                    tb.HasTrigger("bgdShelf_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_Shelf_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.ShelfName)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<ShelfSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_ShelfSection");

            entity.ToTable("ShelfSection", "bgd", tb =>
                {
                    tb.HasTrigger("bgdShelfSection_DeleteInsteadOf");
                    tb.HasTrigger("bgdShelfSection_InsertAfter");
                    tb.HasTrigger("bgdShelfSection_UpdateInteadOf");
                });

            entity.HasIndex(e => e.Gid, "AK_bgd_ShelfSection_GID").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Blocked).HasDefaultValue(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdShelf).HasColumnName("FK_bgd_Shelf");
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.HeightCm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.SectionName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.WidthCm).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.FkBgdShelfNavigation).WithMany(p => p.ShelfSections)
                .HasForeignKey(d => d.FkBgdShelf)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_bgd_ShelfSection__bgd_Shelf");
        });

        modelBuilder.Entity<MarkerAdditionalType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_bgd_MarkerAdditionalType");

            entity.ToTable("MarkerAdditionalType", "bgd", tb =>
            {
                tb.HasTrigger("bgdMarkerAdditionalType_DeleteInsteadOf");
                tb.HasTrigger("bgdMarkerAdditionalType_InsertAfter");
                tb.HasTrigger("bgdMarkerAdditionalType_UpdateInteadOf");
            });

            entity.HasIndex(e => e.Gid, "AK_bgd_MarkerAdditionalType_GID").IsUnique();

            entity.HasIndex(e => e.TypeDesc, "UQ_bgd_MarkerAdditionalType_TypeDesc").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("GID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });


        modelBuilder.Entity<VwBoardGame>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGame", "bgd");

            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.BoardGameSummary).IsUnicode(false);
            entity.Property(e => e.BoardGameType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ComplexityRating).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGameType).HasColumnName("FK_bgd_BoardGameType");
            entity.Property(e => e.FkBgdBoardGameVictoryConditionType).HasColumnName("FK_bgd_BoardGameVictoryConditionType");
            entity.Property(e => e.FkBgdPublisher).HasColumnName("FK_bgd_Publisher");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.HeightCm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.HowToPlayHyperlink).IsUnicode(false);
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LocationCode).HasMaxLength(35);
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.WidthCm).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<VwBoardGameEloMethod>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameEloMethod", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.ExpectedWinRatioTeamA).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdEloMethod).HasColumnName("FK_bgd_EloMethod");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameImageType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameImageType", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameMarker>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameMarker", "bgd");

            entity.Property(e => e.BoardGameMarkerTypeTypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("BoardGameMarkerType_TypeDesc");
            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdBoardGameMarkerType).HasColumnName("FK_bgd_BoardGameMarkerType");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameMarkerType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameMarkerType", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameMatch>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameMatch", "bgd");

            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdResultType).HasColumnName("FK_bgd_ResultType");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.ResultTypeTypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ResultType_TypeDesc");
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameMatchPlayer>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameMatchPlayer", "bgd");

            entity.Property(e => e.BoardGameMarkerTypeTypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("BoardGameMarkerType_TypeDesc");
            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGameMarker).HasColumnName("FK_bgd_BoardGameMarker");
            entity.Property(e => e.FkBgdBoardGameMatch).HasColumnName("FK_bgd_BoardGameMatch");
            entity.Property(e => e.FkBgdPlayer).HasColumnName("FK_bgd_Player");
            entity.Property(e => e.FullName)
                .HasMaxLength(123)
                .IsUnicode(false);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameMatchPlayerResult>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameMatchPlayerResult", "bgd");

            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FinalScore).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.FkBgdBoardGameMatchPlayer).HasColumnName("FK_bgd_BoardGameMatchPlayer");
            entity.Property(e => e.FkBgdResultType).HasColumnName("FK_bgd_ResultType");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.ResultTypeTypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ResultType_TypeDesc");
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameNight>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameNight", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameNightBoardGameMatch>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameNightBoardGameMatch", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGameMatch).HasColumnName("FK_bgd_BoardGameMatch");
            entity.Property(e => e.FkBgdBoardGameNight).HasColumnName("FK_bgd_BoardGameNight");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameNightPlayer>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameNightPlayer", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGameNight).HasColumnName("FK_bgd_BoardGameNight");
            entity.Property(e => e.FkBgdPlayer).HasColumnName("FK_bgd_Player");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameResult>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameResult", "bgd");

            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdResultType).HasColumnName("FK_bgd_ResultType");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.ResultTypeTypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ResultType_TypeDesc");
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameShelfSection>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameShelfSection", "bgd");

            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdShelfSection).HasColumnName("FK_bgd_ShelfSection");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LocationCode).HasMaxLength(35);
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameType", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwBoardGameVictoryConditionType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwBoardGameVictoryConditionType", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwEloMethod>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwEloMethod", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.MethodDescription).HasMaxLength(255);
            entity.Property(e => e.MethodName).HasMaxLength(128);
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwGameHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwGameHistory", "bgd");

            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwGameNightPlayerPoint>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwGameNightPlayerPoints", "bgd");

            entity.Property(e => e.BoardGameId).HasColumnName("BoardGameID");
            entity.Property(e => e.GameNightId).HasColumnName("GameNightID");
            entity.Property(e => e.MatchId).HasColumnName("MatchID");
            entity.Property(e => e.PlayerId).HasColumnName("PlayerID");
            entity.Property(e => e.PlayerScore).HasColumnType("decimal(9, 2)");
        });

        modelBuilder.Entity<VwGameNightPlayerRanking>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwGameNightPlayerRankings", "bgd");

            entity.Property(e => e.GameNightId).HasColumnName("GameNightID");
            entity.Property(e => e.OverallRank).HasMaxLength(10);
            entity.Property(e => e.PlayerId).HasColumnName("PlayerID");
            entity.Property(e => e.PlayerName)
                .HasMaxLength(123)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwMarkerAlignmentType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwMarkerAlignmentType", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwPlayer>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwPlayer", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FirstName)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(122)
                .IsUnicode(false);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LastName)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.MiddleName)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwPlayerBoardGameRating>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwPlayerBoardGameRating", "bgd");

            entity.Property(e => e.BoardGameName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdBoardGame).HasColumnName("FK_bgd_BoardGame");
            entity.Property(e => e.FkBgdPlayer).HasColumnName("FK_bgd_Player");
            entity.Property(e => e.FullName)
                .HasMaxLength(123)
                .IsUnicode(false);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.Rating).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwPublisher>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwPublisher", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Description).IsUnicode(false);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.PublisherName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwRankingQueryStore>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwRankingQueryStore", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.RankingQueryStoreName)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.ViewName).IsUnicode(false);
        });

        modelBuilder.Entity<VwResultType>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwResultType", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.TypeDesc)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwShelf>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwShelf", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.ShelfName)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<VwShelfLocationView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwShelfLocationView", "bgd");

            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Location).HasMaxLength(66);
            entity.Property(e => e.SectionName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ShelfName)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.WidthCm).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<VwShelfSection>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwShelfSection", "bgd");

            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.FkBgdShelf).HasColumnName("FK_bgd_Shelf");
            entity.Property(e => e.Gid).HasColumnName("GID");
            entity.Property(e => e.HeightCm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID");
            entity.Property(e => e.ModifiedBy).HasMaxLength(128);
            entity.Property(e => e.SectionName)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.TimeCreated).HasColumnType("datetime");
            entity.Property(e => e.TimeModified).HasColumnType("datetime");
            entity.Property(e => e.VersionStamp)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.WidthCm).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<PlayerNameResult>(entity =>
        {
            entity.HasNoKey();
            entity.ToFunction("ReturnPlayerName", builder =>
            {
                builder.HasSchema("bgd");
            });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    [DbFunction("ReturnPlayerName", "bgd")]
    public IQueryable<PlayerNameResult> ReturnPlayerName(long playerId)
    {
        return FromExpression(() => ReturnPlayerName(playerId));
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
