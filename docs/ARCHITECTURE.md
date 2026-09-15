# Architecture

## Overview

Tiny Tower is a client-heavy Canvas game (TypeScript + Vite, no framework)
backed by Firebase — Hosting for the static build, Cloud Functions for a
thin `/api/*` HTTPS surface, Firestore for leaderboards/daily/challenges,
and Firebase Auth (anonymous) for guest identity. The game is fully
playable offline; only competitive/social features require the network.

```
┌─────────────────────────────┐        ┌───────────────────────────────┐
│  Browser (PWA)                │        │  Firebase Hosting              │
│  ─────────────────────────    │  HTTP  │  ─────────────────────         │
│  src/game    (engine, sim)    │◄──────►│  rewrites "/api/**" to  ──┐    │
│  src/ui      (DOM overlays)   │  JSON  │  the Cloud Function        │    │
│  src/services(ApiClient,      │        └────────────────────────────┼───┘
│               Firebase Auth)  │                                     │
│  src/storage (localStorage)   │        ┌────────────────────────────▼───┐
└─────────────────────────────┘        │  Cloud Function: api (Express)  │
                                        │  ─────────────────────          │
                                        │  functions/src/routes           │
                                        │  functions/src/services         │
                                        │  functions/src/lib (auth,       │
                                        │   rate limit, anti-cheat)        │
                                        │           │                     │
                                        │           ▼                     │
                                        │   Firestore + Firebase Auth      │
                                        └──────────────────────────────────┘
```

## Why this split

- **Determinism first.** Gameplay math (overlap, scoring, combo, level
  progression, difficulty scaling, seeded RNG) lives in plain TypeScript
  modules with no DOM/Canvas dependency, so it can be unit tested directly
  and — critically — the *exact same code* runs inside the Cloud Function
  to re-validate a submitted score, rather than a hand-copied duplicate
  that could silently drift. `firebase/functions/src/lib/gameRules.ts`
  re-exports `src/scoring`, `src/levels`, and a few pure `src/utils`
  modules straight from the client's own source tree — see
  §Anti-cheat model.
- **Replaceable backend.** All network access goes through
  `src/services/ApiClient.ts` and typed service classes (`PlayerService`,
  `LeaderboardService`, `ChallengeService`, `DailyService`,
  `AnalyticsService`). Swapping Firebase for something else later means
  rewriting `firebase/functions` + these service implementations, not the
  game — no gameplay file has ever imported Firebase directly.
- **Storage abstraction.** `src/storage/LocalStorage.ts` wraps
  `window.localStorage` behind a small interface so preferences (sound,
  haptics, cached nickname, local bests) never leak raw `localStorage`
  calls throughout the codebase, and so this can be swapped for another
  persistence layer later.

## Directory layout

```
src/
  branding.ts            Single source of truth for game name/strings
  game/                  Orchestration: config, state, loop, top-level Game
  engine/                Renderer, Camera, Input, Audio, Haptics, Particles,
                         EffectsManager, CameraShake, TimeScaleManager, TrailRenderer
  entities/              Block, Tower, FallingPiece (pure geometry/physics)
  levels/                LevelDefinition, 20 handcrafted levels, DifficultyEngine,
                         levelSequence (server-shared level reconstruction)
  scoring/               ScoreEngine, ComboSystem (pure, tested)
  modes/                 Classic / Endless / Daily / Challenge mode adapters
  ui/                     HUD, StartScreen, GameOverScreen, Leaderboard, ChallengeScreen,
                         theme/environment + per-level hue progression, skin interfaces
  services/              firebase.ts (Auth), ApiClient + typed service wrappers
  storage/               LocalStorage abstraction
  utils/                 RNG/seed, math, color, nickname, id generation
  types/                 Shared TypeScript interfaces

firebase/
  firebase.json           Hosting (public: dist, "/api/**" rewrite), Functions, Firestore, emulators
  .firebaserc             Project alias — replace with your own Firebase project id
  firestore.rules          Locks the DB to server-only writes; public leaderboard reads
  firestore.indexes.json   (empty — every leaderboard query is a single-field orderBy)
  functions/
    src/index.ts            Exports the single `api` HTTPS Cloud Function (Express app)
    src/app.ts               Express app: cors, json body limit, route mounting, error handler
    src/routes/              player, session, leaderboard, daily, challenge
    src/lib/                 firestore (Admin SDK init), auth (ID token verification),
                             gameRules (re-exports client scoring/levels code),
                             revalidate (anti-cheat), rateLimit, date
    src/services/            leaderboardService (Firestore read/write helpers)
    src/validation/          zod request schemas
    tests/                   Vitest tests for revalidateRun (pure, no Firestore needed)

public/                   Static assets: manifest, icons, offline shell
tests/                    Vitest unit tests for gameplay + validation logic
docs/                     This documentation set
```

## Game loop

`GameLoop` drives a single `requestAnimationFrame` loop with a fixed-step
accumulator for simulation (movement, difficulty timers) decoupled from
variable-rate rendering, so physics stay consistent across refresh rates.
`Game.ts` owns:

1. current `LevelDefinition`/`DifficultyEngine` output,
2. the `Tower` (stack of placed blocks) and the current moving `Block`,
3. the `Camera`,
4. `ScoreEngine` + `ComboSystem` state,
5. dispatch of discrete events (placement, cut, perfect, near-miss,
   combo milestone, record, game over) to `engine/EffectsManager`, which
   owns all "juice" (particles, shake, slow-mo, audio, haptics) — Game.ts
   never decides how something looks, only that it happened.

UI overlays (score, buttons, screens) are plain DOM/CSS positioned above
the canvas — cheap to build accessibly and only re-rendered on state
changes, keeping the canvas free to focus purely on the animated scene.

## Modes as adapters

A "mode" only decides: which seed to use, which sequence of
`LevelDefinition`s (or `DifficultyEngine` curve) to draw from, and what an
"end of run" means (Classic ends after level 20 → unlocks Endless; Endless
never ends by itself; Daily/Challenge end after either a miss or hitting
the compared score). Everything else reuses the same `Game` instance.

## Anti-cheat model

See `docs/SECURITY.md` for full detail. In short:

1. `POST /api/session/start` — the Cloud Function issues a `sessionId`.
   For **Daily Tower**, the seed is always derived server-side from the
   UTC date (`dailySeed(utcDateKey(), GAME_VERSION)`) — a client can never
   supply its own seed for Daily. Other modes may pass a client-generated
   seed (Classic/Endless) or a friend's seed (Challenge).
2. The client plays and accumulates a compact placement trace
   (`PlacementResult[]` — index, floor, accuracy, grade, overlap/width,
   score gained, running total, timestamp).
3. `POST /api/session/complete` — the Function loads the session in a
   Firestore transaction and checks: session exists, belongs to the
   caller (verified Firebase Auth uid, never a client-asserted id),
   hasn't already been completed (blocks duplicate submissions), hasn't
   expired (45 min TTL), and that `seed`/`gameVersion`/`mode` match what
   was issued.
4. `revalidateRun()` (`firebase/functions/src/lib/revalidate.ts`)
   reconstructs the exact `LevelDefinition` active at each placement index
   from the seed alone (`buildLevelSequence` — deterministic, since every
   recorded placement succeeded), then replays the trace through the same
   `ScoreEngine` the client uses. A submission is rejected if the
   recomputed running score, final score, perfect count, best combo, or
   best perfect streak don't match, if `height` doesn't equal the
   placement count, if any overlap width exceeds its block's width, or if
   the average time per placement is faster than is physically plausible.
5. Only a validated run updates `leaderboard_alltime` /
   `leaderboard_daily` / `leaderboard_weekly` (one doc per player per
   window — see `docs/DATABASE.md`) and the player's aggregate stats.

Rules version is stamped on every summary (`RULES_VERSION` in
`src/branding.ts`) so old scores stay interpretable if scoring ever
changes.

## Skins architecture

`ui/skins/TowerSkin.ts`, `ui/skins/EnvironmentTheme.ts`, and
`ui/skins/ParticleTheme.ts` define small config interfaces (colors,
gradients, particle palette) consumed by the renderer. Gameplay code never
reads skin data — cosmetics can never affect physics/scoring. The default
skin additionally gets a smooth per-level hue shift (`ui/theme/levelHue.ts`
+ `ui/theme/blockColor.ts`), baked into each `Block`/`FallingPiece` at
creation time rather than repainted globally — see `docs/GAME_DESIGN.md`.
Purchased skins (Neon/Ice/Gold/...) opt out of the hue shift entirely and
keep a fixed palette. A skin store can be added later purely as UI + a
purchased-skins list on the player profile.

## Data flow for a competitive run

```
StartScreen → Game (mode=Classic|Endless|Daily|Challenge)
  → services/firebase.ts: ensureSignedIn() (anonymous Firebase Auth, cached)
  → services/ApiClient: POST /api/session/start  (seed, sessionId)
  → gameplay loop (client-authoritative rendering, server-verifiable trace)
  → GameOverScreen
  → services/ApiClient: POST /api/session/complete (trace + claimed result)
  → LeaderboardService / DailyService / ChallengeService as applicable
```

If the network is unavailable, the run still completes locally; the
result is queued (`storage/LocalStorage` pending-sync list, via
`services/RunSubmitter.ts`) and flushed on the next `online` event — see
§Error Handling in `docs/DEPLOYMENT.md`.

## Guest identity

Firebase Anonymous Auth (`signInAnonymously`) creates a stable `uid` on
first launch with zero interruption before first play, and Firebase
persists that session across reloads — the same device keeps the same
player. The uid *is* the player id everywhere, client and server; a
nickname is the only thing the player chooses. Upgrading to a real
account later (email, Google, etc.) is a matter of linking a credential
to this same uid — no data migration needed.
