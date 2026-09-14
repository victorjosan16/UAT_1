import { randomId } from "@/utils/rng";
import type { GameMode } from "@/types";

export interface ModeConfig {
  mode: GameMode;
  seed: string;
  targetScoreToBeat: number | null;
}

/** Levels 1-20 in order; Game.ts auto-transitions into Endless afterwards. */
export function createClassicRun(): ModeConfig {
  return { mode: "CLASSIC", seed: `classic:${randomId(10)}`, targetScoreToBeat: null };
}
