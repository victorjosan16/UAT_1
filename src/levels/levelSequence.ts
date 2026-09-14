import { getLevel, MAX_LEVEL } from "./levels";
import { DifficultyEngine } from "./DifficultyEngine";
import type { LevelDefinition } from "./LevelDefinition";

/**
 * Reconstructs which LevelDefinition was active for each of the first
 * `count` successful placements of a run, given only the seed. Every
 * recorded placement succeeded (a miss ends the run without being
 * recorded), so the level-advance schedule is a pure function of
 * placement count — no player-performance-dependent state is needed.
 *
 * Used server-side (see firebase/functions) to re-derive each
 * placement's `levelScoreMultiplier` for anti-cheat re-scoring, without
 * trusting anything the client claims about which level it was on.
 * Mirrors the live per-placement bookkeeping in game/Game.ts
 * (`levelDefFor` + `blocksPlacedInLevel`) — keep the two in sync.
 */
export function buildLevelSequence(seed: string, count: number, startLevel = 1): LevelDefinition[] {
  const difficultyEngine = new DifficultyEngine(seed);
  const levelDefFor = (level: number): LevelDefinition => (level <= MAX_LEVEL ? getLevel(level) : difficultyEngine.definitionForFloor(level - MAX_LEVEL));

  const sequence: LevelDefinition[] = [];
  let level = startLevel;
  let blocksInLevel = 0;
  let currentDef = levelDefFor(level);

  for (let i = 0; i < count; i++) {
    sequence.push(currentDef);
    blocksInLevel += 1;
    if (blocksInLevel >= currentDef.requiredBlocks) {
      level += 1;
      blocksInLevel = 0;
      currentDef = levelDefFor(level);
    }
  }
  return sequence;
}
