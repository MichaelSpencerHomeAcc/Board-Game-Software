using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Board_Game_Software.Pages.GameNight
{
    public class DetailsModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly BoardGameImagesService _images; // existing service (covers)
        private readonly IMongoCollection<BoardGameImages> _imagesCollection; // for player avatars

        public DetailsModel(BoardGameDbContext db, BoardGameImagesService images, IMongoClient mongo, IConfiguration config)
        {
            _db = db;
            _images = images;

            // set up Mongo collection for player images
            var dbName = config["MongoDbSettings:Database"];
            var database = mongo.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public BoardGameNight Night { get; private set; } = null!;
        public List<MatchRow> Matches { get; private set; } = new();
        public List<PlayerRow> Players { get; private set; } = new();

        public sealed class MatchRow
        {
            public long MatchId { get; init; }
            public string GameName { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public string? CoverDataUrl { get; init; } // base64 data URL or null
        }

        public sealed class PlayerRow
        {
            public long PlayerId { get; init; }
            public Guid PlayerGid { get; init; }
            public string Name { get; init; } = string.Empty;
            public string? AvatarDataUrl { get; init; } // base64 data URL or null
        }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            // Night + matches + games
            Night = await _db.BoardGameNights
                .Include(n => n.BoardGameNightBoardGameMatches)
                    .ThenInclude(nm => nm.FkBgdBoardGameMatchNavigation)
                        .ThenInclude(m => m.FkBgdBoardGameNavigation)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (Night == null) return NotFound();

            // ===== Matches (covers) =====
            var bgList = Night.BoardGameNightBoardGameMatches
                .Select(nm => nm.FkBgdBoardGameMatchNavigation?.FkBgdBoardGameNavigation)
                .Where(bg => bg != null)
                .Distinct()!
                .ToList();

            var frontTypeGid = await _db.BoardGameImageTypes
                .AsNoTracking()
                .Where(t => t.TypeDesc == "Board Game Front")
                .Select(t => t.Gid)
                .FirstOrDefaultAsync();

            var coverMap = frontTypeGid == Guid.Empty
                ? new Dictionary<Guid, string?>()
                : await _images.GetFrontImagesAsync(bgList.Select(bg => bg!.Gid), frontTypeGid);

            Matches = Night.BoardGameNightBoardGameMatches
                .Select(nm =>
                {
                    var match = nm.FkBgdBoardGameMatchNavigation!;
                    var bg = match.FkBgdBoardGameNavigation!;
                    return new MatchRow
                    {
                        MatchId = match.Id,
                        GameName = bg.BoardGameName,
                        GameGid = bg.Gid,
                        CoverDataUrl = coverMap.GetValueOrDefault(bg.Gid)
                    };
                })
                .ToList();

            // ===== Players (avatars) =====
            // Get active players for this night
            var roster = await _db.BoardGameNightPlayers
                .AsNoTracking()
                .Where(x => x.FkBgdBoardGameNight == id && !x.Inactive)
                .Select(x => x.FkBgdPlayerNavigation)
                .ToListAsync();

            // Batch fetch player images from Mongo: SQLTable == "bgd.Player" AND GID IN (...)
            var playerGids = roster.Select(p => (Guid?)p.Gid).ToArray(); // nullable because Mongo model is Guid?
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.In(x => x.GID, playerGids)
            );

            var docs = await _imagesCollection.Find(filter).ToListAsync();
            var avatarMap = docs.ToDictionary(
                d => d.GID!.Value, // safe: filtered by IN on non-null values
                d => d.ImageBytes != null && !string.IsNullOrWhiteSpace(d.ContentType)
                        ? $"data:{d.ContentType};base64,{Convert.ToBase64String(d.ImageBytes)}"
                        : null
            );

            Players = roster
                .Select(p => new PlayerRow
                {
                    PlayerId = p.Id,
                    PlayerGid = p.Gid,
                    Name = string.Join(" ", new[] { p.FirstName, p.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    AvatarDataUrl = avatarMap.GetValueOrDefault(p.Gid)
                })
                .OrderBy(p => p.Name)
                .ToList();

            return Page();
        }

        // ==========================
        // Delete handlers (Admin + unfinished only)
        // ==========================

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteMatchAsync(long id, long matchId)
        {
            // Night + permission checks (unchanged)
            var night = await _db.BoardGameNights
                .Include(n => n.BoardGameNightBoardGameMatches)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (night == null) return NotFound();
            if (!User.IsInRole("Admin")) return Forbid();
            if (night.Finished) return BadRequest("Cannot delete from a finished night.");

            // Ensure this match belongs to the night (unchanged)
            var link = await _db.BoardGameNightBoardGameMatches
                .FirstOrDefaultAsync(x => x.FkBgdBoardGameNight == id && x.FkBgdBoardGameMatch == matchId);
            if (link == null) return NotFound();

            await using var tx = await _db.Database.BeginTransactionAsync();

            // 1) Load match players and their ids
            var matchPlayers = await _db.BoardGameMatchPlayers
                .Where(p => p.FkBgdBoardGameMatch == matchId)
                .ToListAsync();
            var mpIds = matchPlayers.Select(p => p.Id).ToList();

            // 2) Delete player results FIRST (to satisfy FK)
            if (mpIds.Count > 0)
            {
                var results = await _db.BoardGameMatchPlayerResults
                    .Where(r => mpIds.Contains(r.FkBgdBoardGameMatchPlayer))
                    .ToListAsync();
                _db.BoardGameMatchPlayerResults.RemoveRange(results);
            }

            // 3) Delete match players
            _db.BoardGameMatchPlayers.RemoveRange(matchPlayers);

            // 4) Remove the link for THIS night -> match
            _db.BoardGameNightBoardGameMatches.Remove(link);

            // 5) If no other nights link to this match, delete the match itself
            var stillLinked = await _db.BoardGameNightBoardGameMatches
                .AnyAsync(x => x.FkBgdBoardGameMatch == matchId && x.FkBgdBoardGameNight != id);

            if (!stillLinked)
            {
                var match = await _db.BoardGameMatches.FirstOrDefaultAsync(m => m.Id == matchId);
                if (match != null) _db.BoardGameMatches.Remove(match);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return RedirectToPage("/GameNight/Details", new { id });
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteNightAsync(long id)
        {
            // Load the night with its links
            var night = await _db.BoardGameNights
                .Include(n => n.BoardGameNightPlayers)
                .Include(n => n.BoardGameNightBoardGameMatches)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (night == null) return NotFound();
            if (!User.IsInRole("Admin")) return Forbid();
            if (night.Finished) return BadRequest("Cannot delete a finished night.");

            await using var tx = await _db.Database.BeginTransactionAsync();

            // Gather match ids linked to this night
            var matchIds = night.BoardGameNightBoardGameMatches
                .Select(x => x.FkBgdBoardGameMatch)
                .Distinct()
                .ToList();

            // Remove players for those matches
            var allMatchPlayers = await _db.BoardGameMatchPlayers
                .Where(p => matchIds.Contains(p.FkBgdBoardGameMatch))
                .ToListAsync();
            _db.BoardGameMatchPlayers.RemoveRange(allMatchPlayers);

            // Remove the links from this night
            _db.BoardGameNightBoardGameMatches.RemoveRange(night.BoardGameNightBoardGameMatches);

            // Remove matches that are no longer linked to any other night
            var matches = await _db.BoardGameMatches
                .Where(m => matchIds.Contains(m.Id))
                .ToListAsync();

            foreach (var m in matches)
            {
                var hasOtherNight = await _db.BoardGameNightBoardGameMatches
                    .AnyAsync(x => x.FkBgdBoardGameMatch == m.Id && x.FkBgdBoardGameNight != id);
                if (!hasOtherNight)
                    _db.BoardGameMatches.Remove(m);
            }

            // Remove roster links
            if (night.BoardGameNightPlayers?.Any() == true)
                _db.BoardGameNightPlayers.RemoveRange(night.BoardGameNightPlayers);

            // Finally remove the night itself
            _db.BoardGameNights.Remove(night);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return RedirectToPage("/GameNight/Index");
        }
    }
}
