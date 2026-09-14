# Database (Firestore)

Tiny Tower uses Cloud Firestore in Native mode. Every collection below is
written **only** by the Cloud Functions Admin SDK (`firebase/functions`),
never directly by the client — see `firebase/firestore.rules` and
`docs/SECURITY.md`. Leaderboard collections are the one exception readable
directly by the client (public data), though in practice the client still
reads them through `GET /api/leaderboard/:window` for consistency.

## Design principle: one doc per player per leaderboard window

Rather than storing every raw score submission and querying/aggregating
at read time (expensive at scale in Firestore, which has no `GROUP BY`),
each leaderboard collection holds **at most one document per player** —
their personal best for that window. A new validated run only overwrites
it if it beats the existing value. This makes every leaderboard read a
plain `orderBy("score", "desc").limit(50)` on a single collection — no
composite indexes needed (`firestore.indexes.json` is intentionally
empty).

## Collections

### `players/{uid}`
```
nickname: string
createdAt: Timestamp
lastSeenAt: Timestamp
totalTowersBuilt: number
bestPerfectStreak: number
bestAverageAccuracy: number
dailyStreak: number
lastDailyDateKey: string        // "YYYY-MM-DD", UTC
```
`{uid}` is the Firebase Auth uid (anonymous or upgraded later) — never a
client-chosen id.

### `sessions/{sessionId}`
Anti-cheat session record; see `docs/SECURITY.md`.
```
playerId: string                // uid, set at /session/start
mode: "CLASSIC" | "ENDLESS" | "DAILY" | "CHALLENGE"
seed: string
gameVersion: string
rulesVersion: number
status: "active" | "completed"
createdAt: Timestamp
expiresAt: Timestamp            // createdAt + 45 minutes
completedAt: Timestamp | null
validationStatus: "accepted" | "rejected" | null
validatedScore: number
validatedHeight: number
validatedPerfectCount: number
validatedAverageAccuracy: number
rejectionReason: string | null
```

### `leaderboard_alltime/{uid}`
```
nickname: string
score: number
height: number
updatedAt: Timestamp
```

### `leaderboard_daily/{dateKey}/entries/{uid}`
Same shape as `leaderboard_alltime`, scoped under a `dateKey` ("YYYY-MM-DD",
UTC) document — this is the general "Today" leaderboard **window** (any
mode's validated run counts), distinct from Daily Tower's own seed-based
tracking below.

### `leaderboard_weekly/{weekKey}/entries/{uid}`
Same shape, scoped under an ISO week key ("2026-W03", UTC).

### `daily_meta/{dateKey}`
```
dateKey: string
seed: string                     // dailySeed(dateKey, GAME_VERSION) — canonical, server-derived
```

### `daily_meta/{dateKey}/players/{uid}`
Per-player stats for that day's specific Daily Tower seed (distinct from
the general "Today" leaderboard window above).
```
bestFloor: number
bestScore: number
perfectCount: number
attemptCount: number
updatedAt: Timestamp
```

### `challenges/{challengeId}`
```
creatorPlayerId: string
creatorNickname: string
creatorScore: number
creatorHeight: number
seed: string
gameVersion: string
createdAt: Timestamp
expiresAt: Timestamp            // createdAt + 7 days
```
`challengeId` is an 8-character unambiguous-alphabet id (see
`src/utils/rng.ts` `randomId`), used directly in the shareable
`/challenge/:id` URL.

### `challenges/{challengeId}/attempts/{uid}`
```
score: number
height: number
updatedAt: Timestamp
```
One document per opponent who has attempted the challenge — only their
best attempt is kept.

### `rate_limits/{bucketId}`
Internal fixed-window rate-limit counters (`firebase/functions/src/lib/rateLimit.ts`).
```
windowStart: Timestamp
count: number
```

## Nothing here is authoritative on the client

`src/storage/LocalStorage.ts` caches a copy of the player's own bests for
instant UI (start screen, offline play) but is never trusted for anything
competitive — every number shown on a leaderboard, in a challenge result,
or in Daily Tower rankings comes from a `GET`/`POST` round-trip to the
Cloud Function, which reads/writes Firestore directly with the Admin SDK.
