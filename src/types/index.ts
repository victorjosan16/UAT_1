export interface Interval {
  left: number;
  right: number;
}

export type Grade = "PERFECT" | "GREAT" | "GOOD" | "RISKY";

export type Direction = 1 | -1;

export type SpecialModifier =
  | "NONE"
  | "WIND"
  | "SPEED_SHIFT"
  | "SMALL_START"
  | "PRECISION"
  | "REVERSE"
  | "DOUBLE_SPEED"
  | "FOG"
  | "MOVING_BASE";

export type DirectionPattern = "CONSTANT" | "ALTERNATING" | "VARIABLE";

export type GameMode = "CLASSIC" | "ENDLESS" | "DAILY" | "CHALLENGE";

export type GameStatus = "IDLE" | "READY" | "PLAYING" | "GAME_OVER";

/** One placement outcome — the unit the scoring engine and the trace both use. */
export interface PlacementResult {
  index: number;
  floor: number;
  accuracy: number;
  grade: Grade;
  isPerfect: boolean;
  perfectStreak: number;
  comboMultiplier: number;
  overlapWidth: number;
  blockWidthBefore: number;
  blockWidthAfter: number;
  scoreGained: number;
  totalScore: number;
  timestampMs: number;
}

export interface RunSummary {
  mode: GameMode;
  seed: string;
  gameVersion: string;
  rulesVersion: number;
  score: number;
  height: number;
  perfectCount: number;
  bestCombo: number;
  bestPerfectStreak: number;
  averageAccuracy: number;
  placements: PlacementResult[];
  durationMs: number;
}
