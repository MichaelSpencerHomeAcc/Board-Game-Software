using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Board_Game_Software.Models;
using Board_Game_Software.Services;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Board_Game_Software.Pages.Match
{
    public class CreateMatchModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly BoardGameImagesService _images;
        private readonly IMongoCollection<BoardGameImages> _imagesCollection;

        public CreateMatchModel(BoardGameDbContext db,
                                BoardGameImagesService images,
                                IMongoClient mongo,
                                IConfiguration config)
        {
            _db = db;
            _images = images;

            // Re-use the same Mongo database as your other pages
            var dbName = config["MongoDbSettings:Database"];
            var database = mongo.GetDatabase(dbName);
            _imagesCollection = database.GetCollection<BoardGameImages>("BoardGameImages");
        }

        public DateOnly? NightDate { get; private set; }
        public List<GameRow> Games { get; private set; } = new();
        public List<NightPlayerRow> Players { get; private set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // ---------- View rows ----------
        public sealed class GameRow
        {
            public long Id { get; set; }
            public Guid Gid { get; set; }
            public string Name { get; set; } = string.Empty;
            public byte? MinMinutes { get; set; }
            public byte? MaxMinutes { get; set; }
            public decimal? Complexity { get; set; }
            public string? CoverDataUrl { get; set; } // from Mongo via service
        }

        public sealed class NightPlayerRow
        {
            public long PlayerId { get; set; }
            public Guid PlayerGid { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool Preselected { get; set; }
            public string? AvatarDataUrl { get; set; } // from Mongo
        }

        // ---------- Markers DTOs (AJAX + binding) ----------
        public sealed class MarkerDto
        {
            public long Id { get; set; }              // BoardGameMarker.Id (the pick to save)
            public string Name { get; set; } = "";    // from BoardGameMarkerType.TypeDesc
            public string? ImageDataUrl { get; set; } // from Mongo via MarkerType.ImageId (optional)
        }

        public sealed class PlayerMarkerInput
        {
            public long PlayerId { get; set; }
            public long? MarkerId { get; set; } // null/empty => None
        }

        public sealed class InputModel
        {
            [Required] public long NightId { get; set; }
            public string? ReturnUrl { get; set; }

            [Required(ErrorMessage = "Please select a game.")]
            public long? BoardGameId { get; set; }

            [Required] public DateOnly MatchDate { get; set; }

            public List<long> SelectedPlayerIds { get; set; } = new();

            // Existing
            public List<PlayerMarkerInput> PlayerMarkers { get; set; } = new();

            // NEW: JSON posted from the client (we'll hydrate PlayerMarkers from this)
            public string? PlayerMarkersJson { get; set; }
        }


        private async Task<string?> TryGetMarkerImageDataUrlAsync(Guid markerTypeGid)
        {
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                Builders<BoardGameImages>.Filter.Eq(x => x.GID, (Guid?)markerTypeGid)
            );

            var doc = await _imagesCollection.Find(filter).FirstOrDefaultAsync();
            if (doc?.ImageBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(doc.ContentType))
            {
                var b64 = Convert.ToBase64String(doc.ImageBytes);
                return $"data:{doc.ContentType};base64,{b64}";
            }
            return null;
        }

        // ==========================
        // GET
        // ==========================
        public async Task<IActionResult> OnGetAsync(long nightId, string? returnUrl = null)
        {
            var night = await _db.BoardGameNights.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nightId);
            if (night == null) return NotFound();

            Input.NightId = nightId;
            Input.ReturnUrl = returnUrl;
            Input.MatchDate = night.GameNightDate;
            NightDate = night.GameNightDate;

            // ----- Games (covers via BoardGameImagesService) -----
            var games = await _db.BoardGames
                .AsNoTracking()
                .Where(g => !g.Inactive)
                .OrderBy(g => g.BoardGameName)
                .Select(g => new { g.Id, g.Gid, g.BoardGameName, g.PlayingTimeMinInMinutes, g.PlayingTimeMaxInMinutes, g.ComplexityRating })
                .ToListAsync();

            // get the "Board Game Front" GUID, then pull images in one shot
            var frontTypeGid = await _db.BoardGameImageTypes
                .AsNoTracking()
                .Where(t => t.TypeDesc == "Board Game Front")
                .Select(t => t.Gid)
                .FirstOrDefaultAsync();

            var coverMap = frontTypeGid == Guid.Empty
                ? new Dictionary<Guid, string?>()
                : await _images.GetFrontImagesAsync(games.Select(x => x.Gid), frontTypeGid);

            Games = games.Select(x => new GameRow
            {
                Id = x.Id,
                Gid = x.Gid,
                Name = x.BoardGameName,
                MinMinutes = x.PlayingTimeMinInMinutes,
                MaxMinutes = x.PlayingTimeMaxInMinutes,
                Complexity = x.ComplexityRating,
                CoverDataUrl = coverMap.GetValueOrDefault(x.Gid)
            }).ToList();

            // ----- Players (avatars via Mongo collection) -----
            var roster = await _db.BoardGameNightPlayers
                .AsNoTracking()
                .Where(r => r.FkBgdBoardGameNight == nightId && !r.Inactive)
                .Select(r => r.FkBgdPlayerNavigation)
                .ToListAsync();

            var playerGids = roster.Select(p => (Guid?)p.Gid).ToArray();
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(d => d.SQLTable, "bgd.Player"),
                Builders<BoardGameImages>.Filter.In(d => d.GID, playerGids)
            );

            var docs = await _imagesCollection.Find(filter).ToListAsync();
            var avatarMap = docs.ToDictionary(
                d => d.GID!.Value,
                d => d.ImageBytes != null && !string.IsNullOrWhiteSpace(d.ContentType)
                    ? $"data:{d.ContentType};base64,{Convert.ToBase64String(d.ImageBytes)}"
                    : null
            );

            Players = roster
                .Select(p => new NightPlayerRow
                {
                    PlayerId = p.Id,
                    PlayerGid = p.Gid,
                    Name = string.Join(" ", new[] { p.FirstName, p.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    Preselected = false,
                    AvatarDataUrl = avatarMap.GetValueOrDefault(p.Gid)
                })
                .OrderBy(p => p.Name)
                .ToList();

            return Page();
        }

        // ==========================
        // AJAX: markers for a selected game
        // GET /Match/Create?handler=Markers&gameId=123
        // ==========================
        public async Task<IActionResult> OnGetMarkersAsync(long nightId, long gameId)
        {
            // Get the markers for this board game
            var markers = await _db.BoardGameMarkers
                .AsNoTracking()
                .Where(m => !m.Inactive && m.FkBgdBoardGame == gameId)
                .Include(m => m.FkBgdBoardGameMarkerTypeNavigation)
                .Select(m => new
                {
                    MarkerId = m.Id, // << IMPORTANT: the FK points to THIS table
                    Name = m.FkBgdBoardGameMarkerTypeNavigation.TypeDesc,
                    TypeGid = m.FkBgdBoardGameMarkerTypeNavigation.Gid
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            // Batch images by Type GID
            var gids = markers.Select(x => (Guid?)x.TypeGid).ToArray();
            var filter = Builders<BoardGameImages>.Filter.And(
                Builders<BoardGameImages>.Filter.Eq(x => x.SQLTable, "bgd.BoardGameMarkerType"),
                Builders<BoardGameImages>.Filter.In(x => x.GID, gids)
            );
            var docs = await _imagesCollection.Find(filter).ToListAsync();
            var imgMap = docs.ToDictionary(
                d => d.GID!.Value,
                d => d.ImageBytes != null && !string.IsNullOrWhiteSpace(d.ContentType)
                    ? $"data:{d.ContentType};base64,{Convert.ToBase64String(d.ImageBytes)}"
                    : null
            );

            // Shape to the JSON your JS expects
            var result = markers.Select(x => new
            {
                id = x.MarkerId,              // << this is the BoardGameMarker.Id
                name = x.Name,
                imageDataUrl = x.TypeGid != Guid.Empty ? imgMap.GetValueOrDefault(x.TypeGid) : null
            });

            return new JsonResult(result);
        }
        
        // ==========================
        // POST
        // ==========================
        public async Task<IActionResult> OnPostAsync()
        {
            if (Input.SelectedPlayerIds == null) Input.SelectedPlayerIds = new();

            if (!ModelState.IsValid)
            {
                await OnGetAsync(Input.NightId, Input.ReturnUrl);
                return Page();
            }

            if (Input.BoardGameId is null || !Input.SelectedPlayerIds.Any())
            {
                if (Input.BoardGameId is null)
                    ModelState.AddModelError(nameof(Input.BoardGameId), "Please select a game.");
                if (!Input.SelectedPlayerIds.Any())
                    ModelState.AddModelError(string.Empty, "Select at least one player.");

                await OnGetAsync(Input.NightId, Input.ReturnUrl);
                return Page();
            }

            if (Input.PlayerMarkers == null || Input.PlayerMarkers.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(Input.PlayerMarkersJson))
                {
                    try
                    {
                        Input.PlayerMarkers =
                            JsonSerializer.Deserialize<List<PlayerMarkerInput>>(Input.PlayerMarkersJson)
                            ?? new List<PlayerMarkerInput>();
                    }
                    catch
                    {
                        // Optional: surface a friendly error
                        ModelState.AddModelError(string.Empty, "Could not read selected markers.");
                    }
                }
            }

            var now = DateTime.UtcNow;
            var who = User?.Identity?.Name ?? "system";

            var match = new BoardGameMatch
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                FkBgdBoardGame = Input.BoardGameId.Value,
                MatchDate = Input.MatchDate,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = who,
                ModifiedBy = who
            };
            _db.Add(match);
            await _db.SaveChangesAsync();

            _db.Add(new BoardGameNightBoardGameMatch
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                FkBgdBoardGameNight = Input.NightId,
                FkBgdBoardGameMatch = match.Id,
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = who,
                ModifiedBy = who
            });

            var links = Input.SelectedPlayerIds.Distinct().Select(pid => new BoardGameMatchPlayer
            {
                Gid = Guid.NewGuid(),
                Inactive = false,
                FkBgdBoardGameMatch = match.Id,
                FkBgdPlayer = pid,
                // FkBgdBoardGameMarker set in SavePlayerMarkersAsync (if provided)
                TimeCreated = now,
                TimeModified = now,
                CreatedBy = who,
                ModifiedBy = who
            });
            _db.AddRange(links);

            await _db.SaveChangesAsync();

            // Persist per-player markers (if any from the form)
            await SavePlayerMarkersAsync(match.Id, Input.PlayerMarkers);

            // Final redirect
            if (!string.IsNullOrWhiteSpace(Input.ReturnUrl))
                return Redirect(Input.ReturnUrl);

            return RedirectToPage("/GameNight/Details", new { id = Input.NightId });
        }

        // ==========================
        // Helpers
        // ==========================
        private async Task SavePlayerMarkersAsync(long matchId, List<PlayerMarkerInput>? choices)
        {
            if (choices == null || choices.Count == 0) return;

            // Deduplicate by player (last wins)
            var byPlayer = choices
                .GroupBy(c => c.PlayerId)
                .Select(g => new { PlayerId = g.Key, MarkerId = g.Last().MarkerId })
                .ToList();

            var playerIds = byPlayer.Select(x => x.PlayerId).Distinct().ToList();

            var rows = await _db.BoardGameMatchPlayers
                .Where(p => p.FkBgdBoardGameMatch == matchId && playerIds.Contains(p.FkBgdPlayer))
                .ToListAsync();

            var now = DateTime.UtcNow;
            var who = User?.Identity?.Name ?? "system";

            foreach (var row in rows)
            {
                var pick = byPlayer.FirstOrDefault(x => x.PlayerId == row.FkBgdPlayer);
                row.FkBgdBoardGameMarker = pick?.MarkerId; // null => None
                row.TimeModified = now;
                row.ModifiedBy = who;
            }

            await _db.SaveChangesAsync();
        }

        private async Task<string?> TryGetMarkerImageDataUrlAsync(string? imageId)
        {
            if (string.IsNullOrWhiteSpace(imageId))
                return null;

            // Adjust field name if your Mongo doc uses a different key than Id/ImageId
            var builder = Builders<BoardGameImages>.Filter;
            var filter = builder.Eq(x => x.Id, imageId) |
                         builder.Eq("ImageId", imageId);

            var doc = await _imagesCollection.Find(filter).FirstOrDefaultAsync();
            if (doc == null || doc.ImageBytes == null || string.IsNullOrWhiteSpace(doc.ContentType))
                return null;

            return $"data:{doc.ContentType};base64,{Convert.ToBase64String(doc.ImageBytes)}";
        }
    }
}
