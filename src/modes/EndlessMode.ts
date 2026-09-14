import { randomId } from "@/utils/rng";
import { MAX_LEVEL } from "@/levels/levels";
import type { GameMode } from "@/types";

export interface EndlessRunConfig {
  mode: GameMode;
  seed: string;
  targetScoreToBeat: null;
  startLevel: number;
}

/** Jumps straight into procedural difficulty, skipping the 20 classic stages. */
export function createEndlessRun(): EndlessRunConfig {
  return { mode: "ENDLESS", seed: `endless:${randomId(10)}`, targetScoreToBeat: null, startLevel: MAX_LEVEL + 1 };
}
