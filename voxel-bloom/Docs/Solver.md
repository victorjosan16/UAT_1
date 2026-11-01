# Level validation & solver

All 100 levels are produced by `tools/build-data.mjs` with deterministic seeds and validated
before they are written.

## What is guaranteed
* **Solvable**: every level stores a `witness` (the board slot tapped at each move). Replaying it
  through the rules wins the level within `moveLimit`. Since the witness *is* a solution, solvability
  is proven per level, not estimated. Checked by `node tools/test-rules.mjs` (JS rules) and by the
  EditMode test `Witness_ReplaysToWin_WithIdenticalMovesAndScore_AllLevels` (C# rules). Identical moves
  and score in both languages prove the two rule implementations agree.
* **Distinct**: no two levels share the same (voxels, deck) configuration (checked at build time and by tests).
* **Structurally valid**: every colour has at least as much attack power in the deck as it has voxels.

## How the witness is found: a heuristic, not an exhaustive search
`solveGreedy` (JS) / `GreedySolver` (C#) picks, at each move, the board block that maximises
immediately useful destruction (prefers colours with exposed voxels and avoids overkill on almost finished
colours). It is **not** a complete search:
* if it fails, the level is regenerated with a new seed, even though a cleverer player might have won;
* it does not prove that a level is *hard*, or that every line of play is winnable (most are not, by design).

## Difficulty calibration
* A casual player model (random taps, preferring playable colours 60% of the time) plays 32–64 games.
* The move limit is binary-searched over the witness length so that this model wins about
  `0.9 - 0.45·d` of games (d = 0 → 1 across the 100 levels).
* **HARD levels (every 5th level)** target 0.3 lower (min 0.15) and pay **x2.5 coins**. Those coins buy
  boosters, notably **Eliminare** (150 coins), which removes a jammed attack from the queue.
* Later chapters add more colours, smaller attack powers (2–64), hidden cores (interior voxels
  recoloured so they surprise the player when exposed) and bigger models.

See `Docs/LevelReport.txt` for per-level numbers (voxels, colours, deck size, witness moves, move limit,
casual win rate, reward).

## Removal order (shared by both implementations)
Within one attack, exposed voxels of the attack colour are removed by: highest `y` first, then
farthest from the vertical axis, then lowest cell index. Exposure is evaluated when the attack fires
and recomputed after each destruction.
