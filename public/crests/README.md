# Real club crests

This folder is where **licensed** club crest images go once usage rights for a
specific club are confirmed (e.g. an official brand-kit download, a license
agreement, or written permission from the club/league).

Until then, every club renders a procedural placeholder crest (shape + color +
initials derived from the club id) — see `src/assets/crestPlaceholder.ts`. No
real crest is used unless it's explicitly wired in here.

## Adding a real crest

1. Drop the licensed image file in this folder, e.g. `real-madrid.svg` (SVG or
   PNG, square aspect ratio, transparent background recommended).
2. In `src/data/clubs.ts`, set `logoUrl: "/crests/real-madrid.svg"` on that
   club's entry.
3. `ClubCrest` automatically renders the real image instead of the
   placeholder for that club, including all reveal modes (FULL/ZOOM/BLUR/
   SILHOUETTE/PIECE).

Do this per club, only once you've actually confirmed you're allowed to use
that specific crest — leave `logoUrl` unset for every club whose rights
aren't cleared yet.
