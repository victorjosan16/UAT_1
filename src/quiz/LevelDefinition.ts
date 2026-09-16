import type { Difficulty, RevealMode } from "@/types";

export interface QuizLevelDefinition {
  level: number;
  questionCount: number;
  timeLimitMs: number;
  minDifficulty: Difficulty;
  maxDifficulty: Difficulty;
  revealMode: RevealMode;
  scoreMultiplier: number;
}

export const QUESTIONS_PER_LEVEL = 10;
export const MAX_LEVEL = 20;

export type DifficultyGroup = "ROOKIE" | "FAN" | "EXPERT" | "MASTER" | "LEGEND";

export function difficultyGroupForLevel(level: number): DifficultyGroup {
  if (level <= 5) return "ROOKIE";
  if (level <= 10) return "FAN";
  if (level <= 15) return "EXPERT";
  if (level <= 19) return "MASTER";
  return "LEGEND";
}
