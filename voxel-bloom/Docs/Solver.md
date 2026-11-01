# Level validation & solver (rules v2: 4 columns, 4 launchers)

All 120 levels are produced by `tools/build-data.mjs` with deterministic seeds and validated before they are written.

## Guaranteed
* **Solvable:** every level stores a `witness` (the column tapped at each move). `replay()` (JS) and
  `GreedySolver.Replay` (C#) apply it as *settle → tap → … → resolve* and must win with the recorded score and
  move count. Identical results in both languages prove the two rule implementations agree.
* **Unique and multicolour:** one distinct object per level, at least 6 colours from the 12-colour palette.
* **Structurally valid:** for every colour the blocks hold at least as many voxels as the model.

## How the witness is found (heuristic, not exhaustive)
`bestColumn` taps the column whose top colour is visible and still needed (prefers colours with many exposed voxels).
A win proves solvability; a failed search only means the heuristic missed — the level is regenerated.

## Difficulty
* **Inner colour layers:** voxels 2+ (and 4+) cells deep get colours that are not on the surface. Their blocks must
  wait in a launcher until digging exposes them.
* **Column order bias:** mostly-hidden colours are pushed towards the top of the columns, so loading them too early
  can jam all 4 launchers.
* A casual player model (random taps, 60% preferring visible colours) plays 24 games per candidate; the candidate
  closest to the target win rate (`0.9 − 0.5·d`, HARD levels 0.3 lower) is kept. See `Docs/LevelReport.txt`.

## Removal order
Highest `y` first, then farthest from the vertical axis, then lowest index (per-colour priority heaps).
