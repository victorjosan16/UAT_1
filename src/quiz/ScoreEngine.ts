import { roundTo } from "@/utils/math";
import type { ClubDifficulty } from "@/types";

export const BASE_SCORE = 500;
const MAX_SPEED_BONUS = 500;
const MAX_DIFFICULTY_BONUS = 300;
const STREAK_BONUS_PER_LEVEL = 60;
const STREAK_BONUS_CAP_COUNT = 10;

export interface AnswerScoreInput {
  correct: boolean;
  /** Time from question shown to answer submitted; ignored (treated as the full time limit) when timed out. */
  responseTimeMs: number;
  timeLimitMs: number;
  difficulty: ClubDifficulty;
  /** The streak count AFTER this answer (0 if wrong/timeout, else previous streak + 1). */
  streakAfter: number;
}

export interface AnswerScoreOutput {
  gained: number;
  speedFraction: number;
  breakdown: {
    base: number;
    speedBonus: number;
    difficultyBonus: number;
    streakBonus: number;
  };
}

/** Fraction of the time limit the player had left when they answered — 1 = instant, 0 = used the whole window. */
export function speedFractionFor(responseTimeMs: number, timeLimitMs: number): number {
  if (timeLimitMs <= 0) return 0;
  return Math.min(1, Math.max(0, 1 - responseTimeMs / timeLimitMs));
}

export function difficultyBonusFor(difficulty: ClubDifficulty): number {
  return Math.round((MAX_DIFFICULTY_BONUS * (difficulty - 1)) / 4);
}

export function streakBonusFor(streakAfter: number): number {
  return Math.min(streakAfter, STREAK_BONUS_CAP_COUNT) * STREAK_BONUS_PER_LEVEL;
}

/** Pure, deterministic per-question score — no Date/Math.random. */
export function scoreAnswer(input: AnswerScoreInput): AnswerScoreOutput {
  if (!input.correct) {
    return { gained: 0, speedFraction: 0, breakdown: { base: 0, speedBonus: 0, difficultyBonus: 0, streakBonus: 0 } };
  }

  const speedFraction = speedFractionFor(input.responseTimeMs, input.timeLimitMs);
  const speedBonus = Math.round(MAX_SPEED_BONUS * speedFraction);
  const difficultyBonus = difficultyBonusFor(input.difficulty);
  const streakBonus = streakBonusFor(input.streakAfter);
  const gained = BASE_SCORE + speedBonus + difficultyBonus + streakBonus;

  return { gained, speedFraction, breakdown: { base: BASE_SCORE, speedBonus, difficultyBonus, streakBonus } };
}

/** Aggregates a full run's per-question scores — used by both client display and (later) server re-validation. */
export class ScoreEngine {
  private total = 0;
  private correctCount = 0;
  private questionCount = 0;
  private responseTimeSumMs = 0;
  private responseTimes: number[] = [];

  score(input: AnswerScoreInput): AnswerScoreOutput {
    const result = scoreAnswer(input);
    this.total += result.gained;
    this.questionCount += 1;
    if (input.correct) {
      this.correctCount += 1;
      this.responseTimeSumMs += input.responseTimeMs;
      this.responseTimes.push(input.responseTimeMs);
    }
    return result;
  }

  get totalScore(): number {
    return this.total;
  }

  get answeredCount(): number {
    return this.questionCount;
  }

  get correctAnswerCount(): number {
    return this.correctCount;
  }

  get averageResponseMs(): number {
    return this.correctCount === 0 ? 0 : roundTo(this.responseTimeSumMs / this.correctCount, 0);
  }

  /** Response times for correctly-answered questions only — used for the Football IQ consistency signal. */
  get correctResponseTimes(): readonly number[] {
    return this.responseTimes;
  }
}
