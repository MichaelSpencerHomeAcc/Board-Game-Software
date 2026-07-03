# EF rebuild deployment notes

Target database: `tironicus_BoardGames_EF_Rebuild`

This folder contains generated SQL for rebuilding the database side by side with the old database.
Do not run these scripts against the existing live database.

## Script order

1. Run `001_identity_migrations_idempotent.sql`
   - Creates ASP.NET Identity tables from `ApplicationDbContextMain`.
   - Safe/idempotent migration script.

2. Run `002_boardgame_model_create.sql`
   - Creates the EF-modeled `bgd` tables from `BoardGameDbContext`.
   - This is model-based SQL, not migration-history SQL, because the current first `BoardGameDbContext` migration has its full schema creation commented out.

3. Run `002b_boardgame_mark_migrations_applied.sql`
   - Marks the current board-game migrations as applied in `__EFMigrationsHistory`.
   - This prevents future EF migration commands from replaying old migrations against the rebuilt schema.

4. Run `002d_fix_active_top_ten_unique_index.sql`
   - Adjusts the `PlayerBoardGame` unique rank index so historical inactive rows can be migrated.
   - New databases created from a regenerated `002_boardgame_model_create.sql` already include this behavior, but running the fix is safe.

5. Run `002c_functional_objects_from_old_db.sql`
   - Recreates functional SQL objects extracted from the old database.
   - `bgd.ReturnPlayerName`
   - other `bgd.*` scalar/table-valued functions used by views
   - `bgd.vw*` views
   - audit triggers are intentionally excluded

6. Run a data-copy script from the old database to `tironicus_BoardGames_EF_Rebuild`.
   Use `003_data_copy_template.sql` as the starting point.
   - The copy script preserves IDs, GUIDs, and relationships.
   - It skips `VersionStamp`.
   - It resets `CreatedBy`, `ModifiedBy`, `TimeCreated`, and `TimeModified` to migration values rather than carrying over the old audit trail.
   - If the source and target databases use separate logins, SQL Server may block cross-database reads. In that case use `Copy-DataBetweenSeparateLogins.ps1` from your machine instead.

7. Run `004_add_club_tenancy_foundation.sql`
   - Adds `bgd.Club` and `bgd.ClubMembership`.
   - Adds nullable `FK_bgd_Club` columns to `bgd.Player` and `bgd.BoardGameNight`.
   - Keeps existing copied rows valid until they are assigned to clubs.

8. Run `005_add_player_club_memberships.sql`
   - Adds `bgd.PlayerClub`, allowing one player to belong to multiple clubs.
   - Backfills memberships from the legacy `bgd.Player.FK_bgd_Club` value where present.

9. Run `006_add_boardgame_club_templates.sql`
   - Adds nullable `FK_bgd_Club` and `FK_bgd_TemplateBoardGame` columns to `bgd.BoardGame`.
   - Existing board games remain global platform templates because `FK_bgd_Club` is null.
   - Future club-owned games can either be custom records or copies linked back to a template.

10. Optional one-off for Mike's Clubhouse: run `007_create_mikes_clubhouse_boardgames_and_repoint_history.sql`
   - Copies every global board-game template into Mike's Clubhouse.
   - Repoints existing game nights, matches, votes, ratings, star ratings, top-10 rows, and marker selections to the club copies.
   - Copies SQL per-game config rows such as ELO methods, results, markers, and expansion links.
   - This does not copy MongoDB board-game images because those are stored outside SQL by game GUID.

11. Validate counts and sample screens before pointing the app at the new database.

12. Revisit audit behavior later.
   - The old trigger-based audit approach is intentionally not part of this first rebuild.
   - Prefer a more explicit application/database audit design before adding replacement triggers.

## Regenerating scripts

From the project root:

```powershell
dotnet ef migrations script --idempotent --context ApplicationDbContextMain --output .\artifacts\sql\001_identity_migrations_idempotent.sql
dotnet ef dbcontext script --context BoardGameDbContext --output .\artifacts\sql\002_boardgame_model_create.sql
```

## Important

`BoardGameDbContext` no longer contains a hardcoded SQL connection string. Configure the target database through `ConnectionStrings:DefaultConnection`, user secrets, environment variables, or your host's secret store.
