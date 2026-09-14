import type { Grade } from "@/types";

export interface ComboState {
  multiplier: number;
  streak: number;
  perfectStreak: number;
  bestCombo: number;
  bestPerfectStreak: number;
}

const MAX_MULTIPLIER = 8;
const MULTIPLIER_STEP = 0.25;

export function initialComboState(): ComboState {
  return { multiplier: 1, streak: 0, perfectStreak: 0, bestCombo: 0, bestPerfectStreak: 0 };
}

/**
 * Pure state transition: given the previous combo state and the grade of
 * the placement that just happened, returns the next state. RISKY resets
 * the multiplier climb (never below 1x) and the perfect streak; any
 * surviving placement keeps the general streak alive.
 */
export function advanceCombo(state: ComboState, grade: Grade, isPerfect: boolean): ComboState {
  const streak = state.streak + 1;
  const perfectStreak = isPerfect ? state.perfectStreak + 1 : 0;

  let multiplier = state.multiplier;
  if (grade === "RISKY") {
    multiplier = 1;
  } else {
    const bump = grade === "PERFECT" ? MULTIPLIER_STEP * 1.5 : MULTIPLIER_STEP;
    multiplier = Math.min(MAX_MULTIPLIER, roundToQuarter(multiplier + bump));
  }

  return {
    multiplier,
    streak,
    perfectStreak,
    bestCombo: Math.max(state.bestCombo, streak),
    bestPerfectStreak: Math.max(state.bestPerfectStreak, perfectStreak),
  };
}

function roundToQuarter(value: number): number {
  return Math.round(value * 4) / 4;
}
