# TINY TOWER — Game Design Document

> Working title: **TINY TOWER**. Subtitle: *BUILD HIGHER. BEAT EVERYONE.*
> All brand strings live in `src/branding.ts` so the name can change without
> touching gameplay code.

## 1. Core Concept

The player builds the tallest possible tower, one block at a time. A block
slides horizontally above the current top of the tower. Tapping the screen
drops the block. Only the part of the new block that overlaps the block
below survives — everything else is cut away and falls. The next block
starts at the width of what remains, so the tower narrows over time and the
game gets progressively harder.

Missing the tower entirely (zero overlap) ends the run.

**Design pillars:** one action, one rule, infinite mastery. Understandable
in ~3 seconds, difficult to truly master.

## 2. Input

- **Mobile:** single tap anywhere on screen.
- **Desktop:** mouse click or `Space`.

No other input is required. Input handling lives in `src/engine/Input.ts`
and normalizes all sources into a single `place` event.

## 3. Placement Physics

On each placement (`src/game/Game.ts` → `entities/Tower.ts`):

1. Freeze the moving block at its current x position.
2. Compute horizontal overlap between the moving block and the top block of
   the tower (`Tower.computeOverlap`).
3. If overlap ≤ 0 → game over.
4. If `|offset| <= perfectTolerance` → **PERFECT**: snap to the full
   footprint of the block below (no width loss).
5. Otherwise, clip the block to the intersection interval. Any remainder
   (left and/or right) becomes a `FallingPiece` with its own simple
   physics (gravity + rotation) for visual feedback.
6. Feed the placement into `ScoreEngine` + `ComboSystem`.
7. Spawn the next moving block at the new (possibly narrower) width.
8. `Camera` eases upward to keep the top of the tower in a comfortable
   viewport position.
9. `DifficultyEngine` / the active `LevelDefinition` adjusts speed, width,
   and tolerance for the next block.

All physics are deterministic and free of external physics engines —
custom, lightweight AABB-interval math is sufficient and keeps the game at
60 FPS on mid-range mobile hardware.

## 4. Precision & Grading

Every placement gets an **accuracy** value in `[0, 100]` and a grade,
computed centrally in `ScoreEngine.gradeForAccuracy`:

| Grade   | Accuracy range |
|---------|-----------------|
| PERFECT | ≥ 99.0          |
| GREAT   | ≥ 90.0          |
| GOOD    | ≥ 75.0          |
| RISKY   | < 75.0          |

Accuracy is `100 * overlapWidth / previousBlockWidth`, with a PERFECT
placement forced to 100 when inside tolerance.

## 5. Scoring

Implemented once, in `src/scoring/ScoreEngine.ts`, never duplicated in UI:

```
placementScore = BASE_SCORE
                + ACCURACY_BONUS(accuracy)
                + HEIGHT_BONUS(floor)
                + PERFECT_BONUS(streak)
runningScore  += placementScore * comboMultiplier
```

- `BASE_SCORE`: flat points for any surviving placement.
- `ACCURACY_BONUS`: scales with accuracy grade.
- `HEIGHT_BONUS`: small, steady bonus that grows with floor number so tall
  towers matter more than early placements.
- `PERFECT_BONUS`: awarded only on PERFECT, scales with the current perfect
  streak (`PERFECT ×2`, `×3`, …), capped to avoid runaway numbers.
- `comboMultiplier`: from `ComboSystem`, increases with consecutive
  non-RISKY placements and resets (not to zero — to 1) on a RISKY grade.

The engine is pure functions over plain data — testable without Canvas.

## 6. Level Progression (1–20)

Levels are data (`src/levels/levels.ts`), not code forks. Each is a
`LevelDefinition`: speed, starting width, speed increase per block,
perfect tolerance, direction pattern, camera speed, optional modifier,
score multiplier, and the number of blocks required to clear the level.

Progression difficulty curve (summary — full data in `levels.ts`):

1–3: slow, wide, generous tolerance — learn the rule.
4: alternating direction introduced.
5–8: steady speed/width ramp, short acceleration bursts.
9–10: tighter tolerance, first checkpoint (mid-boss pacing).
11–14: faster, variable movement, smaller blocks.
15: precision-focused (tight tolerance, moderate speed).
16–17: higher acceleration, advanced movement patterns.
18: very small perfect tolerance.
19: high speed + precision combined.
20: **MASTER TOWER** — hardest handcrafted stage; clearing it unlocks
    **Endless Mode**.

## 7. Endless Mode

After Level 20, difficulty scales procedurally via `DifficultyEngine`,
using smooth continuous functions (with caps) over floor number for speed,
width, tolerance, and modifier frequency — never random-impossible spikes.
Endless tracks personal bests: highest tower, highest floor, highest score,
best perfect streak, best average accuracy (`services/PlayerService.ts` +
worker `player/stats`).

## 8. Modifiers

Modifiers are small, fair, readable twists layered onto a level via
`LevelDefinition.specialModifier`: `WIND`, `SPEED_SHIFT`, `SMALL_START`,
`PRECISION`, `REVERSE`, `DOUBLE_SPEED`, `FOG`, `MOVING_BASE`. Each is a pure
function of time/seed so it never produces an unwinnable frame.

## 9. Modes

- **Classic** (`modes/ClassicMode.ts`): levels 1→20 in order.
- **Endless** (`modes/EndlessMode.ts`): procedural, unlocked after Classic.
- **Daily** (`modes/DailyMode.ts`): one deterministic seed per UTC day
  (`utils/dailySeed.ts`), identical conditions for every player.
- **Challenge** (`modes/ChallengeMode.ts`): a friend's run's seed +
  gameplay version replayed for a head-to-head comparison.

All modes drive the same core `Game` engine — a mode only supplies a seed,
a level/difficulty source, and an end condition.

## 10. Feel: Camera, Particles, Audio, Haptics

- **Camera** eases smoothly upward (no snapping), independent of gameplay
  math (`engine/Camera.ts`).
- **Particles** are a small pooled system (`engine/Particles.ts`) used for
  PERFECT bursts, combo milestones, records, and game-over dust. Particle
  count adapts down automatically if frame time budget is exceeded
  (reduced-effects mode).
- **Audio** (`engine/Audio.ts`) is fully procedural via the Web Audio API
  (oscillators/envelopes) — no binary audio assets to fetch, small bundle,
  works offline. Perfect-streak tones rise in pitch with streak length.
- **Haptics** (`engine/Haptics.ts`) uses `navigator.vibrate` behind a
  feature check; distinct short/long/patterned pulses for normal, perfect,
  combo, and failure.

Both are toggleable and persisted via the storage abstraction, never
required for gameplay.

## 11. Visual Progression

The environment theme is a pure function of tower height
(`ui/environmentForHeight`), not asset-heavy: gradients, palette, and
particle density shift smoothly through Ground → City → Sky → Clouds →
Upper Atmosphere → Space bands, interpolated rather than switched abruptly.

## 12. Retention & Virality

- Milestone toasts at 10/25/50/75/100/150/200 floors.
- Daily streak counter.
- **Challenge a Friend**: after a run, the client asks the API to mint a
  `challengeId` bound to the run's score/height/seed; sharing the link lets
  a friend replay the identical seed and see a head-to-head result.
  See `docs/API.md` for the contract and `docs/SECURITY.md` for anti-cheat.
- All numbers shown to players (ranks, percentiles, leaderboards) come from
  real stored data — never fabricated.

## 13. Non-Goals for v1

Cosmetic skin *store* UI, account/auth beyond guest identity, and
matchmaking are out of scope for v1 but the architecture (see
`docs/ARCHITECTURE.md` §Skins) leaves room for them.
