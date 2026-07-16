using Board_Game_Software.Models;
using Board_Game_Software.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Board_Game_Software.Pages.Match
{
    public class OpenModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly ICurrentClubService _currentClubService;

        public OpenModel(BoardGameDbContext db, ICurrentClubService currentClubService)
        {
            _db = db;
            _currentClubService = currentClubService;
        }

        public List<OpenMatchRow> Matches { get; private set; } = new();
        public string ScopeLabel { get; private set; } = "Open one-off matches";

        public sealed class OpenMatchRow
        {
            public long MatchId { get; init; }
            public string GameName { get; init; } = string.Empty;
            public Guid GameGid { get; init; }
            public DateTime? MatchDate { get; init; }
            public string ContextLabel { get; init; } = string.Empty;
            public string Visibility { get; init; } = string.Empty;
            public int ParticipantCount { get; init; }
            public string Participants { get; init; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            var currentClub = await _currentClubService.GetCurrentClubAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = _db.BoardGameMatches
                .AsNoTracking()
                .Include(m => m.FkBgdBoardGameNavigation)
                .Include(m => m.BoardGameMatchPlayers.Where(mp => !mp.Inactive))
                    .ThenInclude(mp => mp.FkBgdPlayerNavigation)
                .Where(m => !m.Inactive
                    && m.MatchComplete != true
                    && m.PlayContext != MatchDefaults.ClubGameNightContext
                    && !m.BoardGameNightBoardGameMatches.Any(link => !link.Inactive));

            if (User.IsInRole("Admin") && currentClub.IsPlatformAdminMode)
            {
                ScopeLabel = "All open one-off matches";
            }
            else if (currentClub.CurrentClubId.HasValue)
            {
                var clubId = currentClub.CurrentClubId.Value;
                query = query.Where(m => m.FkBgdClub == clubId);
                ScopeLabel = $"Open one-off matches for {currentClub.CurrentClubName}";
            }
            else
            {
                query = query.Where(m => m.FkBgdClub == null
                    && !string.IsNullOrWhiteSpace(userId)
                    && m.BoardGameMatchPlayers.Any(mp => !mp.Inactive
                        && mp.FkBgdPlayer.HasValue
                        && mp.FkBgdPlayerNavigation != null
                        && mp.FkBgdPlayerNavigation.FkdboAspNetUsers == userId));
                ScopeLabel = "Your open personal plays";
            }

            var matches = await query
                .OrderByDescending(m => m.MatchDate ?? m.TimeCreated)
                .ThenByDescending(m => m.Id)
                .Take(100)
                .ToListAsync();

            Matches = matches.Select(m =>
            {
                var participants = m.BoardGameMatchPlayers
                    .OrderBy(GetParticipantName)
                    .Select(GetParticipantName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new OpenMatchRow
                {
                    MatchId = m.Id,
                    GameName = m.FkBgdBoardGameNavigation?.BoardGameName ?? "Unknown game",
                    GameGid = m.FkBgdBoardGameNavigation?.Gid ?? Guid.Empty,
                    MatchDate = m.MatchDate,
                    ContextLabel = GetContextLabel(m.PlayContext),
                    Visibility = m.Visibility,
                    ParticipantCount = m.BoardGameMatchPlayers.Count(mp => !mp.Inactive),
                    Participants = participants.Any() ? string.Join(", ", participants.Take(6)) : "No participants"
                };
            }).ToList();
        }

        private static string GetParticipantName(BoardGameMatchPlayer matchPlayer)
        {
            var registeredName = $"{matchPlayer.FkBgdPlayerNavigation?.FirstName} {matchPlayer.FkBgdPlayerNavigation?.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(registeredName)) return registeredName;
            if (!string.IsNullOrWhiteSpace(matchPlayer.GuestName)) return matchPlayer.GuestName;
            if (!string.IsNullOrWhiteSpace(matchPlayer.InvitedEmail)) return matchPlayer.InvitedEmail;
            return "Guest";
        }

        private static string GetContextLabel(string? playContext)
        {
            return playContext switch
            {
                MatchDefaults.PersonalContext => "Personal play",
                MatchDefaults.PrivateGroupContext => "Private group",
                MatchDefaults.ClubOneOffContext => "Club one-off",
                _ => "One-off"
            };
        }
    }
}
