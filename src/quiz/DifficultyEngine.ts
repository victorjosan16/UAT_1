import { roundTo } from "@/utils/math";
import { QUESTIONS_PER_LEVEL, MAX_LEVEL, type QuizLevelDefinition } from "./LevelDefinition";

const ENDLESS_START_TIME_LIMIT_MS = 10000;
const ENDLESS_MIN_TIME_LIMIT_MS = 7000;
const ENDLESS_TIME_DECAY_MS_PER_ROUND = 40;
const ENDLESS_MULTIPLIER_GROWTH_PER_ROUND = 0.015;

/**
 * Procedural difficulty for Endless mode (unlocked after Level 20) — same
 * config-driven shape as the fixed curriculum in levels.ts, just extended
 * indefinitely instead of capped at a hand-picked ceiling.
 */
export class DifficultyEngine {
  definitionForRound(roundIndex: number): QuizLevelDefinition {
    const timeLimitMs = Math.max(ENDLESS_MIN_TIME_LIMIT_MS, Math.round(ENDLESS_START_TIME_LIMIT_MS - roundIndex * ENDLESS_TIME_DECAY_MS_PER_ROUND));
    const scoreMultiplier = roundTo(1.5 + roundIndex * ENDLESS_MULTIPLIER_GROWTH_PER_ROUND, 2);

    return {
      level: MAX_LEVEL + roundIndex,
      questionCount: QUESTIONS_PER_LEVEL,
      timeLimitMs,
      minDifficulty: 3,
      maxDifficulty: 5,
      revealMode: "PIECE",
      scoreMultiplier,
    };
  }
}
