# Scoring

The full design rationale lives in `docs/GAME_DESIGN.md §5-7`; this page
is the quick reference to the actual formulas, all implemented in
`src/scoring/ScoreEngine.ts` and `src/scoring/ComboSystem.ts` — the same
two files run client-side for display and server-side
(`firebase/functions/src/lib/gameRules.ts` re-exports them) for anti-cheat
re-validation, so there is exactly one implementation, never two to keep
in sync.

## Per-placement score

```
placementScore = (BASE_SCORE + ACCURACY_BONUS + HEIGHT_BONUS + PERFECT_BONUS) × levelScoreMultiplier
scoreGained     = round(placementScore × comboMultiplier)
```

| Term | Formula | Notes |
|---|---|---|
| `BASE_SCORE` | `10` | Flat, for any surviving placement |
| `ACCURACY_BONUS` | `round(accuracy × 0.5)` | `accuracy` is 0–100 |
| `HEIGHT_BONUS` | `floor × 2` | Rewards height, not just precision |
| `PERFECT_BONUS` | `min(perfectStreak, 10) × 15`, else `0` | Only on a PERFECT; capped |
| `levelScoreMultiplier` | from the active `LevelDefinition` | `1 + (level-1) × 0.05` in Classic |
| `comboMultiplier` | from `ComboSystem`, `1`–`8` | See below |

## Accuracy and grading

```
accuracy = 100 × overlapWidth / previousBlockWidth      // forced to 100 on a PERFECT snap
```

| Grade | Threshold |
|---|---|
| PERFECT | tolerance-snap only (never from accuracy alone — see below) |
| GREAT | accuracy ≥ 90 |
| GOOD | accuracy ≥ 75 |
| RISKY | accuracy < 75 |

`gradeForPlacement(accuracy, isPerfect)` reserves the `PERFECT` label for
an actual tolerance-snap (`entities/Tower.computeOverlap`); a placement
that happens to score ≥99% accuracy through pure geometry without being
inside tolerance is capped at `GREAT`, not silently promoted to `PERFECT`
(that label carries visual/audio/haptic weight it shouldn't get by
accident).

## Combo multiplier

- Starts at `1`, climbs by `+0.25` per surviving placement
  (`+0.375` on a PERFECT), capped at `8`.
- A `RISKY` grade resets the multiplier to `1` (but the general streak
  counter itself keeps climbing — see `ComboSystem` tests for the exact
  semantics).
- `bestCombo` / `bestPerfectStreak` only ever increase within a run —
  used for the Game Over screen and for the "new record" mid-run
  detection.

## Determinism

Every input to these formulas — `overlapWidth`, `blockWidthBefore`,
`floor`, `isPerfect`, and `levelScoreMultiplier` — is either geometry from
that exact placement or derived purely from the seed + placement index
(`src/levels/levelSequence.ts`). Nothing depends on wall-clock time,
randomness outside the seeded RNG, or any client-only state, which is
what makes server-side re-derivation in `revalidateRun` possible at all
— see `docs/SECURITY.md`.
