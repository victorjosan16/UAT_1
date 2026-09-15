import { describe, expect, it } from "vitest";
import { advanceStreak, initialStreakState, streakTierReached } from "@/quiz/StreakSystem";

describe("advanceStreak", () => {
  it("increments current streak on a correct answer", () => {
    const s1 = advanceStreak(initialStreakState, true);
    expect(s1.current).toBe(1);
    const s2 = advanceStreak(s1, true);
    expect(s2.current).toBe(2);
  });

  it("resets current streak to 0 on a wrong answer", () => {
    let state = initialStreakState;
    state = advanceStreak(state, true);
    state = advanceStreak(state, true);
    state = advanceStreak(state, false);
    expect(state.current).toBe(0);
  });

  it("tracks the best streak even after a reset", () => {
    let state = initialStreakState;
    for (let i = 0; i < 5; i++) state = advanceStreak(state, true);
    state = advanceStreak(state, false);
    expect(state.best).toBe(5);
    expect(state.current).toBe(0);
  });
});

describe("streakTierReached", () => {
  it("returns the tier label exactly when the threshold is first reached", () => {
    expect(streakTierReached(3)).toBe("STREAK!");
    expect(streakTierReached(5)).toBe("ON FIRE!");
    expect(streakTierReached(10)).toBe("UNSTOPPABLE!");
  });

  it("returns null between thresholds", () => {
    expect(streakTierReached(1)).toBeNull();
    expect(streakTierReached(4)).toBeNull();
    expect(streakTierReached(7)).toBeNull();
    expect(streakTierReached(11)).toBeNull();
  });
});
