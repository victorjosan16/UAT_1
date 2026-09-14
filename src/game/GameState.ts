import type { ComboState } from "@/scoring/ComboSystem";
import { initialComboState } from "@/scoring/ComboSystem";
import type { GameMode, GameStatus, PlacementResult } from "@/types";

export interface GameState {
  status: GameStatus;
  mode: GameMode;
  seed: string;
  level: number; // 1-20 during Classic; keeps incrementing conceptually in Endless
  floor: number;
  score: number;
  combo: ComboState;
  perfectCount: number;
  placements: PlacementResult[];
  startedAtMs: number;
  targetScoreToBeat: number | null; // Challenge/Daily comparison target
}

export function createInitialState(mode: GameMode, seed: string, targetScoreToBeat: number | null = null): GameState {
  return {
    status: "READY",
    mode,
    seed,
    level: 1,
    floor: 0,
    score: 0,
    combo: initialComboState(),
    perfectCount: 0,
    placements: [],
    startedAtMs: 0,
    targetScoreToBeat,
  };
}
