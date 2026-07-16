using Board_Game_Software.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Board_Game_Software.Pages.Admin.Clubs
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly BoardGameDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(BoardGameDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public List<ClubRow> Clubs { get; set; } = new();
        public SelectList UserOptions { get; set; } = default!;
        public SelectList RoleOptions { get; set; } = default!;
        public List<SelectListItem> ClubTypeOptions { get; set; } = new();
        public List<SelectListItem> VisibilityOptions { get; set; } = new();
        public ClubAdminSummary Summary { get; set; } = new();

        [BindProperty] public ClubInput Input { get; set; } = new();
        [BindProperty] public long ClubId { get; set; }
        [BindProperty] public string? UserId { get; set; }
        [BindProperty] public string? InvitedEmail { get; set; }
        [BindProperty] public string? GuestName { get; set; }
        [BindProperty] public string? MembershipRole { get; set; }
        [BindProperty] public long MembershipId { get; set; }

        [TempData] public string? StatusMessage { get; set; }

        public sealed class ClubInput
        {
            public long? Id { get; set; }
            public string ClubName { get; set; } = string.Empty;
            public string? Slug { get; set; }
            public string? Description { get; set; }
            public string? ContactEmail { get; set; }
            public string? VenueName { get; set; }
            public string? VenueAddress { get; set; }
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
            public string ClubType { get; set; } = ClubDefaults.PublicClubType;
            public string Visibility { get; set; } = ClubDefaults.PublicVisibility;
            public bool IsDiscoverable { get; set; } = true;
            public bool AllowJoinRequests { get; set; } = true;
            public string DefaultGameNightVisibility { get; set; } = ClubDefaults.PublicVisibility;
            public string DefaultMatchVisibility { get; set; } = ClubDefaults.PublicVisibility;
            public bool ShowStatsPublicly { get; set; } = true;
            public string? OwnerUserId { get; set; }
            public bool CreateStarterShelf { get; set; } = true;
        }

        public sealed class ClubRow
        {
            public long Id { get; init; }
            public string ClubName { get; init; } = string.Empty;
            public string? Slug { get; init; }
            public string? Description { get; init; }
            public string? ContactEmail { get; init; }
            public string? VenueName { get; init; }
            public string? VenueAddress { get; init; }
            public decimal? Latitude { get; init; }
            public decimal? Longitude { get; init; }
            public string ClubType { get; init; } = ClubDefaults.PublicClubType;
            public string Visibility { get; init; } = ClubDefaults.PublicVisibility;
            public bool IsDiscoverable { get; init; }
            public bool AllowJoinRequests { get; init; }
            public string DefaultGameNightVisibility { get; init; } = ClubDefaults.PublicVisibility;
            public string DefaultMatchVisibility { get; init; } = ClubDefaults.PublicVisibility;
            public bool ShowStatsPublicly { get; init; }
            public int PlayerCount { get; init; }
            public int BoardGameCount { get; init; }
            public int ShelfCount { get; init; }
            public int GameNightCount { get; init; }
            public int OpenGameNightCount { get; init; }
            public DateOnly? LastGameNightDate { get; init; }
            public bool HasContact => !string.IsNullOrWhiteSpace(ContactEmail);
            public bool HasVenue => !string.IsNullOrWhiteSpace(VenueName);
            public bool HasMapLocation => Latitude.HasValue && Longitude.HasValue;
            public string ClubTypeLabel => ClubDefaults.GetDisplayName(ClubType);
            public string VisibilityLabel => ClubDefaults.GetDisplayName(Visibility);
            public string DefaultGameNightVisibilityLabel => ClubDefaults.GetDisplayName(DefaultGameNightVisibility);
            public string DefaultMatchVisibilityLabel => ClubDefaults.GetDisplayName(DefaultMatchVisibility);
            public List<string> Owners { get; init; } = new();
            public List<MemberRow> Members { get; init; } = new();
        }

        public sealed class ClubAdminSummary
        {
            public int TotalClubs { get; init; }
            public int PublicClubs { get; init; }
            public int PrivateGroups { get; init; }
            public int HiddenClubs { get; init; }
            public int ClubsNeedingOwner { get; init; }
        }

        public sealed class MemberRow
        {
            public long Id { get; init; }
            public string? UserId { get; init; }
            public string DisplayName { get; init; } = string.Empty;
            public string Role { get; init; } = string.Empty;
            public string Status { get; init; } = ClubMembershipDefaults.ActiveStatus;
            public DateTime JoinedAt { get; init; }
            public string RoleLabel => ClubMembershipDefaults.GetDisplayName(Role);
            public string StatusLabel => ClubMembershipDefaults.GetDisplayName(Status);
        }

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        public async Task<IActionResult> OnPostSaveClubAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.ClubName))
            {
                ModelState.AddModelError("Input.ClubName", "Club name is required.");
            }

            var slug = BuildSlug(Input.Slug, Input.ClubName);
            var slugExists = await _db.Clubs.AnyAsync(c => c.Slug == slug && c.Id != Input.Id);
            if (slugExists)
            {
                ModelState.AddModelError("Input.Slug", "That slug is already used by another club.");
            }

            NormalizeVisibilityInput();

            if (!ClubDefaults.IsValidClubType(Input.ClubType))
            {
                ModelState.AddModelError("Input.ClubType", "Choose a valid club type.");
            }

            if (!ClubDefaults.IsValidVisibility(Input.Visibility))
            {
                ModelState.AddModelError("Input.Visibility", "Choose a valid club visibility.");
            }

            if (!ClubDefaults.IsValidVisibility(Input.DefaultGameNightVisibility))
            {
                ModelState.AddModelError("Input.DefaultGameNightVisibility", "Choose a valid game night visibility.");
            }

            if (!ClubDefaults.IsValidVisibility(Input.DefaultMatchVisibility))
            {
                ModelState.AddModelError("Input.DefaultMatchVisibility", "Choose a valid match visibility.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync();
                return Page();
            }

            var now = DateTime.UtcNow;
            var actor = User.Identity?.Name ?? "system";
            Club club;

            if (Input.Id.HasValue)
            {
                club = await _db.Clubs.FirstOrDefaultAsync(c => c.Id == Input.Id.Value)
                    ?? throw new InvalidOperationException("Club not found.");
            }
            else
            {
                club = new Club
                {
                    CreatedBy = actor,
                    TimeCreated = now
                };
                _db.Clubs.Add(club);
            }

            club.ClubName = Input.ClubName.Trim();
            club.Slug = slug;
            club.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
            club.ContactEmail = string.IsNullOrWhiteSpace(Input.ContactEmail) ? null : Input.ContactEmail.Trim();
            club.VenueName = string.IsNullOrWhiteSpace(Input.VenueName) ? null : Input.VenueName.Trim();
            club.VenueAddress = string.IsNullOrWhiteSpace(Input.VenueAddress) ? null : Input.VenueAddress.Trim();
            club.Latitude = Input.Latitude;
            club.Longitude = Input.Longitude;
            club.ClubType = Input.ClubType;
            club.Visibility = Input.Visibility;
            club.IsDiscoverable = Input.IsDiscoverable;
            club.AllowJoinRequests = Input.AllowJoinRequests;
            club.DefaultGameNightVisibility = Input.DefaultGameNightVisibility;
            club.DefaultMatchVisibility = Input.DefaultMatchVisibility;
            club.ShowStatsPublicly = Input.ShowStatsPublicly;
            club.ModifiedBy = actor;
            club.TimeModified = now;

            var isNewClub = !Input.Id.HasValue;
            await _db.SaveChangesAsync();

            if (isNewClub && !string.IsNullOrWhiteSpace(Input.OwnerUserId))
            {
                await UpsertMembershipAsync(club.Id, Input.OwnerUserId, "Owner", actor, now);
            }

            if (isNewClub && Input.CreateStarterShelf)
            {
                var hasShelf = await _db.Shelves.AnyAsync(s => !s.Inactive && s.FkBgdClub == club.Id);
                if (!hasShelf)
                {
                    _db.Shelves.Add(new Shelf
                    {
                        Gid = Guid.NewGuid(),
                        Inactive = false,
                        ShelfName = "Main Shelf",
                        TotalRows = 1,
                        FkBgdClub = club.Id,
                        CreatedBy = actor,
                        ModifiedBy = actor,
                        TimeCreated = now,
                        TimeModified = now
                    });
                    await _db.SaveChangesAsync();
                }
            }

            StatusMessage = Input.Id.HasValue ? "Club updated." : "Club created.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddMemberAsync()
        {
            if (ClubId <= 0)
            {
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(MembershipRole) || !ClubMembershipDefaults.IsValidRole(MembershipRole))
            {
                MembershipRole = ClubMembershipDefaults.MemberRole;
            }

            var clubExists = await _db.Clubs.AnyAsync(c => c.Id == ClubId);
            if (!clubExists)
            {
                return NotFound();
            }

            var actor = User.Identity?.Name ?? "system";
            var now = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(UserId))
            {
                var user = await _userManager.FindByIdAsync(UserId);
                if (user == null)
                {
                    return NotFound();
                }

                await UpsertMembershipAsync(ClubId, UserId, MembershipRole, actor, now);
                StatusMessage = $"Added {user.Email ?? user.UserName} to the club.";
                return RedirectToPage();
            }

            if (!string.IsNullOrWhiteSpace(InvitedEmail))
            {
                await UpsertInviteAsync(ClubId, InvitedEmail, MembershipRole, actor, _userManager.GetUserId(User), now);
                StatusMessage = $"Invited {InvitedEmail.Trim()} to the club.";
                return RedirectToPage();
            }

            if (!string.IsNullOrWhiteSpace(GuestName))
            {
                await UpsertGuestAsync(ClubId, GuestName, actor, now);
                StatusMessage = $"Added guest {GuestName.Trim()} to the club.";
                return RedirectToPage();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveMemberAsync()
        {
            var membership = await _db.ClubMemberships.FirstOrDefaultAsync(m => m.Id == MembershipId);
            if (membership == null)
            {
                return NotFound();
            }

            membership.Inactive = true;
            membership.Status = ClubMembershipDefaults.RemovedStatus;
            membership.ModifiedBy = User.Identity?.Name ?? "system";
            membership.TimeModified = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            StatusMessage = "Club member removed.";
            return RedirectToPage();
        }

        private async Task LoadPageDataAsync()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .Select(u => new { u.Id, Label = u.Email ?? u.UserName ?? u.Id })
                .ToListAsync();

            UserOptions = new SelectList(users, "Id", "Label");
            RoleOptions = new SelectList(
                ClubMembershipDefaults.Roles.Select(role => new
                {
                    Value = role,
                    Label = ClubMembershipDefaults.GetDisplayName(role)
                }),
                "Value",
                "Label");
            ClubTypeOptions = ClubDefaults.ClubTypes
                .Select(type => new SelectListItem(ClubDefaults.GetDisplayName(type), type))
                .ToList();
            VisibilityOptions = ClubDefaults.VisibilityLevels
                .Select(visibility => new SelectListItem(ClubDefaults.GetDisplayName(visibility), visibility))
                .ToList();

            var userLookup = users.ToDictionary(u => u.Id, u => u.Label);
            var clubs = await _db.Clubs
                .AsNoTracking()
                .Where(c => !c.Inactive)
                .Include(c => c.ClubMemberships.Where(m => !m.Inactive))
                .OrderBy(c => c.ClubName)
                .ToListAsync();

            var playerCounts = await _db.PlayerClubs
                .AsNoTracking()
                .Where(pc => !pc.Inactive && !pc.FkBgdPlayerNavigation.Inactive)
                .GroupBy(pc => pc.FkBgdClub)
                .Select(g => new { ClubId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClubId, x => x.Count);

            var gameNightCounts = await _db.BoardGameNights
                .AsNoTracking()
                .Where(n => !n.Inactive && n.FkBgdClub.HasValue)
                .GroupBy(n => n.FkBgdClub!.Value)
                .Select(g => new
                {
                    ClubId = g.Key,
                    Count = g.Count(),
                    OpenCount = g.Count(n => !n.Finished),
                    LastDate = g.Max(n => (DateOnly?)n.GameNightDate)
                })
                .ToDictionaryAsync(x => x.ClubId);

            var boardGameCounts = await _db.BoardGames
                .AsNoTracking()
                .Where(bg => !bg.Inactive && bg.FkBgdClub.HasValue)
                .GroupBy(bg => bg.FkBgdClub!.Value)
                .Select(g => new { ClubId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClubId, x => x.Count);

            var shelfCounts = await _db.Shelves
                .AsNoTracking()
                .Where(s => !s.Inactive && s.FkBgdClub.HasValue)
                .GroupBy(s => s.FkBgdClub!.Value)
                .Select(g => new { ClubId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClubId, x => x.Count);

            Clubs = clubs.Select(c => new ClubRow
            {
                Id = c.Id,
                ClubName = c.ClubName,
                Slug = c.Slug,
                Description = c.Description,
                ContactEmail = c.ContactEmail,
                VenueName = c.VenueName,
                VenueAddress = c.VenueAddress,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                ClubType = c.ClubType,
                Visibility = c.Visibility,
                IsDiscoverable = c.IsDiscoverable,
                AllowJoinRequests = c.AllowJoinRequests,
                DefaultGameNightVisibility = c.DefaultGameNightVisibility,
                DefaultMatchVisibility = c.DefaultMatchVisibility,
                ShowStatsPublicly = c.ShowStatsPublicly,
                PlayerCount = playerCounts.GetValueOrDefault(c.Id),
                BoardGameCount = boardGameCounts.GetValueOrDefault(c.Id),
                ShelfCount = shelfCounts.GetValueOrDefault(c.Id),
                GameNightCount = gameNightCounts.GetValueOrDefault(c.Id)?.Count ?? 0,
                OpenGameNightCount = gameNightCounts.GetValueOrDefault(c.Id)?.OpenCount ?? 0,
                LastGameNightDate = gameNightCounts.GetValueOrDefault(c.Id)?.LastDate,
                Owners = c.ClubMemberships
                    .Where(m => m.Role == ClubMembershipDefaults.OwnerRole &&
                        m.Status == ClubMembershipDefaults.ActiveStatus &&
                        !string.IsNullOrWhiteSpace(m.UserId))
                    .Select(m => userLookup.GetValueOrDefault(m.UserId!, m.UserId!))
                    .ToList(),
                Members = c.ClubMemberships
                    .OrderBy(m => m.Status == ClubMembershipDefaults.ActiveStatus ? 0 : m.Status == ClubMembershipDefaults.InvitedStatus ? 1 : 2)
                    .ThenBy(m => m.Role == ClubMembershipDefaults.OwnerRole ? 0 : m.Role == ClubMembershipDefaults.AdminRole ? 1 : m.Role == ClubMembershipDefaults.MemberRole ? 2 : 3)
                    .ThenBy(m => GetMembershipDisplayName(m, userLookup))
                    .Select(m => new MemberRow
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        DisplayName = GetMembershipDisplayName(m, userLookup),
                        Role = m.Role,
                        Status = m.Status,
                        JoinedAt = m.JoinedAt
                    })
                    .ToList()
            }).ToList();

            Summary = new ClubAdminSummary
            {
                TotalClubs = Clubs.Count,
                PublicClubs = Clubs.Count(c => c.ClubType == ClubDefaults.PublicClubType),
                PrivateGroups = Clubs.Count(c => c.ClubType == ClubDefaults.PrivateGroupType),
                HiddenClubs = Clubs.Count(c => !c.IsDiscoverable || c.Visibility != ClubDefaults.PublicVisibility),
                ClubsNeedingOwner = Clubs.Count(c => !c.Owners.Any())
            };
        }

        private void NormalizeVisibilityInput()
        {
            Input.ClubType = string.IsNullOrWhiteSpace(Input.ClubType)
                ? ClubDefaults.PublicClubType
                : Input.ClubType.Trim();
            Input.Visibility = string.IsNullOrWhiteSpace(Input.Visibility)
                ? ClubDefaults.PublicVisibility
                : Input.Visibility.Trim();
            Input.DefaultGameNightVisibility = string.IsNullOrWhiteSpace(Input.DefaultGameNightVisibility)
                ? ClubDefaults.PublicVisibility
                : Input.DefaultGameNightVisibility.Trim();
            Input.DefaultMatchVisibility = string.IsNullOrWhiteSpace(Input.DefaultMatchVisibility)
                ? ClubDefaults.PublicVisibility
                : Input.DefaultMatchVisibility.Trim();

            if (Input.ClubType == ClubDefaults.PrivateGroupType)
            {
                Input.Visibility = ClubDefaults.PrivateVisibility;
                Input.IsDiscoverable = false;
                Input.AllowJoinRequests = false;
                Input.DefaultGameNightVisibility = ClubDefaults.PrivateVisibility;
                Input.DefaultMatchVisibility = ClubDefaults.PrivateVisibility;
                Input.ShowStatsPublicly = false;
            }
            else if (Input.Visibility != ClubDefaults.PublicVisibility)
            {
                Input.IsDiscoverable = false;
            }
        }

        private static string BuildSlug(string? requestedSlug, string clubName)
        {
            var source = string.IsNullOrWhiteSpace(requestedSlug) ? clubName : requestedSlug;
            var slug = Regex.Replace(source.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..12] : slug;
        }

        private async Task UpsertMembershipAsync(long clubId, string userId, string role, string actor, DateTime now)
        {
            var membership = await _db.ClubMemberships
                .FirstOrDefaultAsync(m => m.FkBgdClub == clubId && m.UserId == userId);

            if (membership == null)
            {
                membership = new ClubMembership
                {
                    FkBgdClub = clubId,
                    UserId = userId,
                    JoinedAt = now,
                    CreatedBy = actor,
                    TimeCreated = now
                };
                _db.ClubMemberships.Add(membership);
            }

            membership.Inactive = false;
            membership.Role = role;
            membership.Status = ClubMembershipDefaults.ActiveStatus;
            membership.GuestName = null;
            membership.InvitedEmail = null;
            membership.InvitedByUserId = null;
            membership.ModifiedBy = actor;
            membership.TimeModified = now;

            await _db.SaveChangesAsync();
        }

        private async Task UpsertInviteAsync(long clubId, string invitedEmail, string role, string actor, string? invitedByUserId, DateTime now)
        {
            var normalizedEmail = invitedEmail.Trim();
            var membership = await _db.ClubMemberships
                .FirstOrDefaultAsync(m => m.FkBgdClub == clubId && m.InvitedEmail == normalizedEmail);

            if (membership == null)
            {
                membership = new ClubMembership
                {
                    FkBgdClub = clubId,
                    InvitedEmail = normalizedEmail,
                    JoinedAt = now,
                    CreatedBy = actor,
                    TimeCreated = now
                };
                _db.ClubMemberships.Add(membership);
            }

            membership.UserId = null;
            membership.GuestName = null;
            membership.Role = role == ClubMembershipDefaults.GuestRole ? ClubMembershipDefaults.MemberRole : role;
            membership.Status = ClubMembershipDefaults.InvitedStatus;
            membership.InvitedByUserId = invitedByUserId;
            membership.Inactive = false;
            membership.ModifiedBy = actor;
            membership.TimeModified = now;

            await _db.SaveChangesAsync();
        }

        private async Task UpsertGuestAsync(long clubId, string guestName, string actor, DateTime now)
        {
            var displayName = guestName.Trim();
            var membership = await _db.ClubMemberships
                .FirstOrDefaultAsync(m => m.FkBgdClub == clubId &&
                    m.GuestName == displayName &&
                    m.Role == ClubMembershipDefaults.GuestRole);

            if (membership == null)
            {
                membership = new ClubMembership
                {
                    FkBgdClub = clubId,
                    GuestName = displayName,
                    JoinedAt = now,
                    CreatedBy = actor,
                    TimeCreated = now
                };
                _db.ClubMemberships.Add(membership);
            }

            membership.UserId = null;
            membership.InvitedEmail = null;
            membership.InvitedByUserId = null;
            membership.Role = ClubMembershipDefaults.GuestRole;
            membership.Status = ClubMembershipDefaults.ActiveStatus;
            membership.Inactive = false;
            membership.ModifiedBy = actor;
            membership.TimeModified = now;

            await _db.SaveChangesAsync();
        }

        private static string GetMembershipDisplayName(ClubMembership membership, IReadOnlyDictionary<string, string> userLookup)
        {
            if (!string.IsNullOrWhiteSpace(membership.UserId))
            {
                return userLookup.GetValueOrDefault(membership.UserId, membership.UserId);
            }

            if (!string.IsNullOrWhiteSpace(membership.InvitedEmail))
            {
                return membership.InvitedEmail;
            }

            if (!string.IsNullOrWhiteSpace(membership.GuestName))
            {
                return membership.GuestName;
            }

            return "Unknown member";
        }
    }
}
