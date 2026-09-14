# API

Single Cloud Function (`api`, an Express app) served behind Firebase
Hosting's `"/api/**"` rewrite (`firebase/firebase.json`), so from the
client's point of view this is just same-origin `/api/*` — no CORS, no
separate hostname to configure. Locally, `npm run emulators` + `npm run dev`
gives the identical routing via the Hosting emulator.

All request/response bodies are JSON. All endpoints marked **Auth**
require `Authorization: Bearer <Firebase ID token>` — `src/services/ApiClient.ts`
attaches this automatically once `ensureSignedIn()` resolves (anonymous
by default). Errors are always `{ "error": string, "code": string }`
with an appropriate HTTP status — never a stack trace (see
`docs/SECURITY.md`).

## Player

### `POST /api/player` — Auth
Upserts the caller's nickname.
```jsonc
// request
{ "nickname": "VICTOR" }
// response
{ "playerId": "uid..." }
```

### `GET /api/player/stats` — Auth
```jsonc
{
  "highScore": 42850,
  "highestFloor": 87,
  "bestPerfectStreak": 23,
  "bestAverageAccuracy": 94.2,
  "totalTowersBuilt": 61
}
```

## Session (anti-cheat)

### `POST /api/session/start` — Auth
```jsonc
// request
{ "mode": "CLASSIC", "seed": "classic:AB12CD34EF" } // seed optional (ignored/overridden for "DAILY")
// response
{ "sessionId": "...", "seed": "...", "gameVersion": "1.0.0", "serverTimeMs": 1234567890 }
```

### `POST /api/session/complete` — Auth
Body is `{ sessionId, summary }` where `summary` matches `RunSummary`
(`src/types/index.ts`) — mode, seed, gameVersion, rulesVersion, score,
height, perfectCount, bestCombo, bestPerfectStreak, averageAccuracy,
`placements[]`, durationMs.
```jsonc
// response (accepted)
{ "accepted": true, "validatedScore": 42850, "rank": { "daily": 12, "weekly": 34, "allTime": 128, "percentile": 4.8 } }
// response (rejected — still 200; the run is simply not counted)
{ "accepted": false, "validatedScore": 0, "rank": { "daily": null, "weekly": null, "allTime": null, "percentile": null } }
```
Error statuses: `404 SESSION_NOT_FOUND`, `403 FORBIDDEN`,
`409 SESSION_ALREADY_COMPLETED`, `410 SESSION_EXPIRED`,
`422 SEED_MISMATCH|VERSION_MISMATCH|MODE_MISMATCH`.

## Leaderboards

### `GET /api/leaderboard/:window` — Auth optional
`:window` is `daily | weekly | all-time`.
```jsonc
{
  "window": "daily",
  "entries": [{ "rank": 1, "playerId": "...", "nickname": "NEXO", "score": 92450, "height": 91 }],
  "you": { "rank": 842, "playerId": "...", "nickname": "PLAYER-7K29", "score": 12500, "height": 30 } // null if you have no entry yet
}
```

## Daily Tower

### `GET /api/daily` — Auth
```jsonc
{
  "dateKey": "2026-03-03",
  "seed": "daily:2026-03-03:1.0.0",
  "todaysBestFloor": 94,
  "yourBestFloor": 72,
  "yourBestScore": 18400,
  "yourRank": 842,
  "attemptCount": 3,
  "streak": 7
}
```

### `POST /api/daily/score` — Auth
Body `{ sessionId }` — the session must already be a **validated**,
`mode: "DAILY"` completed session.
```jsonc
{ "accepted": true, "rank": 840, "streak": 8 }
```

## Challenges

### `POST /api/challenge` — Auth
Body `{ sessionId }` — mints a challenge from an already-validated run.
```jsonc
{ "challengeId": "7KF92A1B", "url": "https://.../challenge/7KF92A1B", "expiresAt": "2026-03-10T12:00:00.000Z" }
```

### `GET /api/challenge/:id` — Auth optional
```jsonc
{
  "challengeId": "7KF92A1B",
  "creatorNickname": "VICTOR",
  "creatorScore": 42850,
  "creatorHeight": 87,
  "seed": "classic:AB12CD34EF",
  "gameVersion": "1.0.0",
  "expired": false
}
```

### `POST /api/challenge/:id/attempt` — Auth
Body `{ sessionId }` — the session must be a validated `mode: "CHALLENGE"`
run whose seed matches the challenge's seed.
```jsonc
{ "accepted": true, "yourScore": 45100, "yourHeight": 90, "opponentScore": 42850, "opponentHeight": 87, "outcome": "WIN" }
```
`outcome` is `"WIN" | "LOSS" | "SO_CLOSE"`.

## Rate limits

`session/start` is limited to 30 requests/minute per uid
(`firebase/functions/src/lib/rateLimit.ts`, a Firestore-transaction-backed
fixed window). This is a blunt anti-spam backstop, not the primary
defense — score integrity comes from `revalidateRun` re-deriving every
number server-side (see `docs/SECURITY.md`).
