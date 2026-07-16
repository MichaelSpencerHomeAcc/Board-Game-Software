# Future Community Architecture

This note captures future community features for Board Game Club Software. It is intentionally design-only: no schema, migration, or implementation work is implied by this document.

## Goals

- Let canonical games become the long-term hub for public community content.
- Let clubs and private groups keep their own private/community content without leaking it publicly.
- Make moderation, reporting, and storage limits first-class design concerns before launch.
- Keep future monetisation hooks aligned with existing tier foundations.

## Content Surfaces

### Game Reviews

Game reviews should attach to canonical shared games, not club-local copies. If a club-added game is later merged into a canonical game, future review prompts should point at the canonical game.

Reviews should support:

- author user id
- canonical game id
- rating or recommendation signal
- review body
- visibility
- moderation status
- edit history or last-edited metadata

Club/private group specific comments about a game should be a separate club-scoped discussion feature, not public game reviews.

### Photos

Photos may attach to several owner types:

- canonical game
- club
- game night
- match

Private group photos should default to private/member-only visibility. Public club photos should still pass moderation before appearing on public pages.

The existing blob storage approach can be extended with owner type, owner id, uploader user id, visibility, moderation state, file size, and derived thumbnail metadata.

### Forums And Comments

Forums may exist at two levels:

- game-level forums for canonical games
- club-level forums for club/private group communities

Game-level forums are public/community content and need moderation from day one. Club forums inherit club visibility and membership rules. Private group forums should never appear in discovery or public search.

Thread and comment design should support soft deletion, locking, pinned posts, report counts, and moderator actions.

## Moderation

Moderation should apply to any user-generated public or semi-public content:

- reviews
- photos
- forum threads
- comments
- profile-visible content
- club descriptions and public event descriptions

Suggested moderation states:

- pending
- approved
- rejected
- hidden
- flagged

Moderation tools should include:

- queue by content type
- queue by report count
- approve/reject/hide actions
- moderator notes
- audit history
- user-level content history

Public launch should not expose unmoderated public uploads or public comments.

## Reporting

Reporting should be available anywhere community content is displayed. Reports should capture:

- reporter user id
- content type
- content id
- reason
- free-text detail
- created timestamp
- resolution status
- resolved by user id

Reports should not delete content directly. They should feed moderation queues.

## Storage Limits

Storage limits should be enforceable by tier and owner scope.

Examples:

- free players: limited profile/gameplay uploads
- player plus: increased personal storage
- private group plus: group photo storage pool
- club basic: modest public club storage
- club pro: larger storage and richer galleries
- venue network: storage across multiple locations

Track usage by owner type and owner id, not just by uploader. This makes it possible to answer “how much storage is this club using?” even when many members upload.

## Achievements

Achievements already exist at a match/player level. Future achievement work should keep achievements event-driven, not hand-calculated from page views.

Future achievement categories:

- play count milestones
- game variety
- club attendance
- teaching games
- tournament wins
- seasonal participation
- collection/library contributions

Achievements should support private visibility for private groups and personal-only achievements.

## Tournaments

Tournaments should build on existing clubs, game nights, matches, participants, and results.

Likely tournament concepts:

- tournament
- tournament round
- table/heat
- standings
- tie-break rules
- bracket or Swiss pairing metadata

Tournament matches should use `tournament_match` so leaderboards and ratings can distinguish them from casual and ordinary scored matches.

## Tier Hooks

The tier foundation should eventually gate:

- whether ads are shown
- advanced personal stats
- private group creation
- club creation
- public club branding
- storage limits
- number of organisers
- advanced tournament tools
- moderation tooling for large clubs or venues

Payments should update tier records through a single service boundary rather than scattering Stripe/payment checks throughout pages.

## Non-Goals For MVP

Do not build these before MVP unless they become launch blockers:

- public game review pages
- public photo galleries
- game forums
- club forums
- advanced moderation dashboards
- full tournament brackets
- seasonal leaderboard resets

The MVP should keep community architecture ready, but focus on reliable club/private group play logging, visibility, stats, and admin cleanup first.
