export interface StreakState {
  current: number;
  best: number;
}

export const initialStreakState: StreakState = { current: 0, best: 0 };

export function advanceStreak(state: StreakState, correct: boolean): StreakState {
  const current = correct ? state.current + 1 : 0;
  return { current, best: Math.max(state.best, current) };
}

export type StreakTierLabel = "STREAK!" | "ON FIRE!" | "UNSTOPPABLE!";

const STREAK_TIERS: readonly { at: number; label: StreakTierLabel }[] = [
  { at: 10, label: "UNSTOPPABLE!" },
  { at: 5, label: "ON FIRE!" },
  { at: 3, label: "STREAK!" },
];

/** Returns the tier label to celebrate exactly when `streak` first reaches that threshold — null on every other count. */
export function streakTierReached(streak: number): StreakTierLabel | null {
  const tier = STREAK_TIERS.find((t) => t.at === streak);
  return tier?.label ?? null;
}
