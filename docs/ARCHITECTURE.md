# Architecture

## Overview

Tiny Tower is a client-heavy Canvas game (TypeScript + Vite, no framework)
backed by a thin Cloudflare Workers API + D1 database for leaderboards,
daily challenges, friend challenges, and lightweight anti-cheat
verification. The game is fully playable offline; only competitive/social
features require the network.

```
┌─────────────────────────────┐        ┌──────────────────────────┐
│  Browser (PWA)               │        │  Cloudflare Worker        │
│  ─────────────────────────   │  HTTP  │  ─────────────────────    │
│  src/game    (engine, sim)   │◄──────►│  worker/routes            │
│  src/ui      (DOM overlays)  │  JSON  │  worker/services          │
│  src/services(ApiClient)     │        │  worker/security          │
│  src/storage (localStorage)  │        │  worker/validation        │
└─────────────────────────────┘        │  worker/db  ──► D1         │
                                        │             ──► KV (rate   │
                                        │                  limiting) │
                                        └──────────────────────────┘
```

## Why this split

- **Determinism first.** Gameplay math (overlap, scoring, combo, level
  progression, difficulty scaling, seeded RNG) lives in plain TypeScript
  modules with no DOM/Canvas dependency, so it can be unit tested directly
  and — critically — the *exact same logic* can be re-run inside the
  Worker to validate a submitted score without trusting the client.
- **Replaceable backend.** All network access goes through
  `src/services/ApiClient.ts` and typed service classes
  (`PlayerService`, `LeaderboardService`, `ChallengeService`,
  `DailyService`, `AnalyticsService`). Swapping Cloudflare Workers/D1 for
  Firebase later means rewriting the Worker + these service
  implementations, not the game.
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
  engine/                Renderer, Camera, Input, Audio, Haptics, Particles
  entities/              Block, Tower, FallingPiece (pure geometry/physics)
  levels/                LevelDefinition, 20 handcrafted levels, DifficultyEngine
  scoring/               ScoreEngine, ComboSystem (pure, tested)
  modes/                 Classic / Endless / Daily / Challenge mode adapters
  ui/                     HUD, StartScreen, GameOverScreen, Leaderboard, ChallengeScreen,
                         theme/environment progression, skin interfaces
  services/              ApiClient + typed service wrappers (network boundary)
  storage/               LocalStorage abstraction
  utils/                 RNG/seed, math, id generation
  types/                 Shared shared TypeScript interfaces

worker/
  index.ts               Router entry point (fetch handler)
  routes/                One module per resource (player, session, leaderboard, …)
  services/              Business logic shared by routes (scoring re-validation, etc.)
  security/              Rate limiting, input limits, Turnstile verification
  validation/            Request payload validation/schemas
  db/                    D1 query helpers, migrations runner glue

migrations/               Numbered SQL migrations for D1
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
5. dispatch to `engine/Particles`, `engine/Audio`, `engine/Haptics` on
   events (placement, perfect, milestone, game over).

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

See `docs/SECURITY.md` for full detail. In short: the Worker issues a
`sessionId` + `seed` + `gameVersion` at session start
(`POST /api/session/start`). The client plays and accumulates a compact
placement trace (index, resulting accuracy/grade, floor). On
`POST /api/session/complete`, the Worker re-derives the score from the
trace using the *same* `scoring`/`levels` logic (compiled once, imported by
both the client bundle and the Worker bundle — no duplicated formulas) and
rejects submissions whose claimed score/height/perfect-count don't match,
whose session is expired/already completed, or whose trace is not
plausible (placement count vs. height, timing bounds).

## Skins architecture (forward-looking)

`ui/skins/TowerSkin.ts`, `ui/skins/EnvironmentTheme.ts`, and
`ui/skins/ParticleTheme.ts` define small config interfaces (colors,
gradients, particle palette) consumed by the renderer. Gameplay code never
reads skin data — cosmetics can never affect physics/scoring. A skin store
can be added later purely as UI + a purchased-skins list on the player
profile.

## Data flow for a competitive run

```
StartScreen → Game (mode=Classic|Endless|Daily|Challenge)
  → services/ApiClient: POST /api/session/start  (seed, sessionId)
  → gameplay loop (client-authoritative rendering, server-verifiable trace)
  → GameOverScreen
  → services/ApiClient: POST /api/session/complete (trace + claimed result)
  → LeaderboardService / DailyService / ChallengeService as applicable
```

If the network is unavailable, the run still completes locally; the result
is queued (`storage/LocalStorage` pending-sync list) and flushed on the
next successful connection — see §Error Handling in `docs/DEPLOYMENT.md`.
