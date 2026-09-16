import { clamp, roundTo } from "@/utils/math";
import type { Difficulty, RevealMode } from "@/types";
import { QUESTIONS_PER_LEVEL, MAX_LEVEL, type QuizLevelDefinition } from "./LevelDefinition";

export { QUESTIONS_PER_LEVEL, MAX_LEVEL };

function revealModeForLevel(level: number): RevealMode {
  if (level <= 7) return "FULL";
  if (level <= 12) return "ZOOM";
  if (level <= 16) return "BLUR";
  return "PIECE";
}

/**
 * Config-driven difficulty (see docs/GAME_DESIGN.md) — every level 1-20 is
 * derived from a single formula over `level`, not hand-authored per level.
 * Adding a Level 21+ curriculum step means adjusting this curve or MAX_LEVEL,
 * never adding a new branch per level. Levels beyond MAX_LEVEL are Endless
 * mode — see DifficultyEngine.ts.
 */
export function getLevel(level: number): QuizLevelDefinition {
  const clamped = clamp(level, 1, MAX_LEVEL);
  const progress = (clamped - 1) / (MAX_LEVEL - 1); // 0 (level 1) .. 1 (level 20)

  const timeLimitMs = Math.round(10000 - progress * 5000); // 10.0s -> 5.0s
  const minDifficulty = clamp(1 + Math.floor(progress * 3), 1, 5) as Difficulty;
  const maxDifficulty = clamp(2 + Math.floor(progress * 4), 2, 5) as Difficulty;
  const scoreMultiplier = roundTo(1 + progress * 0.5, 2); // 1.0x -> 1.5x

  return {
    level: clamped,
    questionCount: QUESTIONS_PER_LEVEL,
    timeLimitMs,
    minDifficulty,
    maxDifficulty,
    revealMode: revealModeForLevel(clamped),
    scoreMultiplier,
  };
}
