import { describe, expect, it } from "vitest";
import { advanceCombo, initialComboState } from "@/scoring/ComboSystem";

describe("advanceCombo", () => {
  it("starts at multiplier 1", () => {
    expect(initialComboState().multiplier).toBe(1);
  });

  it("increases the multiplier on GOOD/GREAT/PERFECT and resets it to 1 on RISKY", () => {
    let state = initialComboState();
    state = advanceCombo(state, "GOOD", false);
    expect(state.multiplier).toBeGreaterThan(1);
    state = advanceCombo(state, "RISKY", false);
    expect(state.multiplier).toBe(1);
  });

  it("caps the multiplier so it never runs away", () => {
    let state = initialComboState();
    for (let i = 0; i < 200; i++) state = advanceCombo(state, "PERFECT", true);
    expect(state.multiplier).toBeLessThanOrEqual(8);
  });

  it("tracks the perfect streak separately from the general streak", () => {
    let state = initialComboState();
    state = advanceCombo(state, "PERFECT", true);
    state = advanceCombo(state, "PERFECT", true);
    state = advanceCombo(state, "GOOD", false);
    expect(state.streak).toBe(3);
    expect(state.perfectStreak).toBe(0); // broken by the non-perfect placement
    expect(state.bestPerfectStreak).toBe(2);
  });

  it("never lets bestPerfectStreak decrease, even once the streak itself is broken", () => {
    // RISKY still survives the placement, so the general streak keeps climbing —
    // only the multiplier and the perfect streak reset.
    let state = initialComboState();
    state = advanceCombo(state, "PERFECT", true);
    state = advanceCombo(state, "PERFECT", true);
    state = advanceCombo(state, "RISKY", false);
    expect(state.streak).toBe(3);
    expect(state.bestCombo).toBe(3);
    expect(state.perfectStreak).toBe(0);
    expect(state.bestPerfectStreak).toBe(2);
  });
});
