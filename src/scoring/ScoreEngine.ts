import type { Grade } from "@/types";
import { roundTo } from "@/utils/math";
import { advanceCombo, initialComboState, type ComboState } from "./ComboSystem";

export const BASE_SCORE = 10;
const ACCURACY_BONUS_FACTOR = 0.5;
const HEIGHT_BONUS_PER_FLOOR = 2;
const PERFECT_BONUS_PER_STREAK = 15;
const PERFECT_STREAK_BONUS_CAP = 10;

export interface GradeThresholds {
  perfect: number;
  great: number;
  good: number;
}

export const GRADE_THRESHOLDS: GradeThresholds = { perfect: 99, great: 90, good: 75 };

/** Accuracy (0-100) purely from overlap geometry — independent of the PERFECT flag. */
export function computeAccuracy(overlapWidth: number, previousWidth: number): number {
  if (previousWidth <= 0) return 0;
  return roundTo(Math.min(100, Math.max(0, (overlapWidth / previousWidth) * 100)), 1);
}

export function gradeForAccuracy(accuracy: number): Grade {
  if (accuracy >= GRADE_THRESHOLDS.perfect) return "PERFECT";
  if (accuracy >= GRADE_THRESHOLDS.great) return "GREAT";
  if (accuracy >= GRADE_THRESHOLDS.good) return "GOOD";
  return "RISKY";
}

/**
 * The single source of truth for placement grading. `isPerfect` reflects
 * the tolerance-snap rule (see entities/Tower.computeOverlap) and always
 * wins; otherwise grade is derived from geometric accuracy, capped below
 * PERFECT so that label stays reserved for true tolerance hits.
 */
export function gradeForPlacement(accuracy: number, isPerfect: boolean): Grade {
  if (isPerfect) return "PERFECT";
  const grade = gradeForAccuracy(accuracy);
  return grade === "PERFECT" ? "GREAT" : grade;
}

export function accuracyBonus(accuracy: number): number {
  return Math.round(accuracy * ACCURACY_BONUS_FACTOR);
}

export function heightBonus(floor: number): number {
  return floor * HEIGHT_BONUS_PER_FLOOR;
}

export function perfectBonus(perfectStreak: number): number {
  if (perfectStreak <= 0) return 0;
  return Math.min(perfectStreak, PERFECT_STREAK_BONUS_CAP) * PERFECT_BONUS_PER_STREAK;
}

export interface PlacementScoreInput {
  accuracy: number;
  grade: Grade;
  isPerfect: boolean;
  floor: number;
  levelScoreMultiplier: number;
  combo: ComboState;
}

export interface PlacementScoreOutput {
  gained: number;
  combo: ComboState;
  breakdown: {
    base: number;
    accuracyBonus: number;
    heightBonus: number;
    perfectBonus: number;
    levelScoreMultiplier: number;
    comboMultiplier: number;
  };
}

/** Deterministic placement score. Pure function — no Date/Math.random. */
export function scorePlacement(input: PlacementScoreInput): PlacementScoreOutput {
  const nextCombo = advanceCombo(input.combo, input.grade, input.isPerfect);

  const base = BASE_SCORE;
  const accBonus = accuracyBonus(input.accuracy);
  const hBonus = heightBonus(input.floor);
  const pBonus = input.isPerfect ? perfectBonus(nextCombo.perfectStreak) : 0;

  const preCombo = (base + accBonus + hBonus + pBonus) * input.levelScoreMultiplier;
  const gained = Math.round(preCombo * nextCombo.multiplier);

  return {
    gained,
    combo: nextCombo,
    breakdown: {
      base,
      accuracyBonus: accBonus,
      heightBonus: hBonus,
      perfectBonus: pBonus,
      levelScoreMultiplier: input.levelScoreMultiplier,
      comboMultiplier: nextCombo.multiplier,
    },
  };
}

/** Aggregates a full run's per-placement scores; used by both client display and server re-validation. */
export class ScoreEngine {
  private total = 0;
  private combo: ComboState = initialComboState();
  private perfectCount = 0;
  private accuracySum = 0;
  private placementCount = 0;

  place(overlapWidth: number, previousWidth: number, floor: number, isPerfect: boolean, levelScoreMultiplier: number): PlacementScoreOutput & { accuracy: number; grade: Grade } {
    const accuracy = computeAccuracy(overlapWidth, previousWidth);
    const grade = gradeForPlacement(accuracy, isPerfect);
    const result = scorePlacement({ accuracy, grade, isPerfect, floor, levelScoreMultiplier, combo: this.combo });

    this.combo = result.combo;
    this.total += result.gained;
    this.placementCount += 1;
    this.accuracySum += accuracy;
    if (isPerfect) this.perfectCount += 1;

    return { ...result, accuracy, grade };
  }

  get totalScore(): number {
    return this.total;
  }

  get comboState(): ComboState {
    return this.combo;
  }

  get perfectPlacementCount(): number {
    return this.perfectCount;
  }

  get averageAccuracy(): number {
    return this.placementCount === 0 ? 0 : roundTo(this.accuracySum / this.placementCount, 1);
  }
}
