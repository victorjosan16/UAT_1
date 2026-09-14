import { describe, expect, it } from "vitest";
import { computeAccuracy, gradeForAccuracy, gradeForPlacement, scorePlacement, ScoreEngine, GRADE_THRESHOLDS } from "@/scoring/ScoreEngine";
import { initialComboState } from "@/scoring/ComboSystem";

describe("computeAccuracy", () => {
  it("is 100 for a full-width overlap", () => {
    expect(computeAccuracy(100, 100)).toBe(100);
  });

  it("is proportional to overlap fraction", () => {
    expect(computeAccuracy(50, 100)).toBe(50);
    expect(computeAccuracy(25, 100)).toBe(25);
  });

  it("returns 0 when previous width is 0 (guards div-by-zero)", () => {
    expect(computeAccuracy(10, 0)).toBe(0);
  });
});

describe("gradeForAccuracy", () => {
  it("matches the documented thresholds", () => {
    expect(gradeForAccuracy(100)).toBe("PERFECT");
    expect(gradeForAccuracy(GRADE_THRESHOLDS.perfect)).toBe("PERFECT");
    expect(gradeForAccuracy(GRADE_THRESHOLDS.perfect - 0.1)).toBe("GREAT");
    expect(gradeForAccuracy(GRADE_THRESHOLDS.great)).toBe("GREAT");
    expect(gradeForAccuracy(GRADE_THRESHOLDS.great - 0.1)).toBe("GOOD");
    expect(gradeForAccuracy(GRADE_THRESHOLDS.good)).toBe("GOOD");
    expect(gradeForAccuracy(GRADE_THRESHOLDS.good - 0.1)).toBe("RISKY");
    expect(gradeForAccuracy(0)).toBe("RISKY");
  });
});

describe("gradeForPlacement", () => {
  it("reserves PERFECT for the tolerance-snap flag, not high accuracy alone", () => {
    // Geometric containment could yield 100% accuracy without tripping the tolerance snap.
    expect(gradeForPlacement(100, false)).toBe("GREAT");
    expect(gradeForPlacement(100, true)).toBe("PERFECT");
  });
});

describe("scorePlacement", () => {
  it("is deterministic: identical input always yields identical output", () => {
    const input = { accuracy: 96, grade: "GREAT" as const, isPerfect: false, floor: 4, levelScoreMultiplier: 1, combo: initialComboState() };
    const a = scorePlacement(input);
    const b = scorePlacement({ ...input });
    expect(a.gained).toBe(b.gained);
    expect(a.combo).toEqual(b.combo);
  });

  it("awards more for PERFECT than an equivalent-accuracy non-perfect placement", () => {
    const combo = initialComboState();
    const perfect = scorePlacement({ accuracy: 100, grade: "PERFECT", isPerfect: true, floor: 5, levelScoreMultiplier: 1, combo });
    const nonPerfect = scorePlacement({ accuracy: 100, grade: "GREAT", isPerfect: false, floor: 5, levelScoreMultiplier: 1, combo });
    expect(perfect.gained).toBeGreaterThan(nonPerfect.gained);
  });

  it("RISKY resets the combo multiplier back to 1", () => {
    let combo = initialComboState();
    for (let i = 0; i < 5; i++) {
      combo = scorePlacement({ accuracy: 100, grade: "PERFECT", isPerfect: true, floor: i, levelScoreMultiplier: 1, combo }).combo;
    }
    expect(combo.multiplier).toBeGreaterThan(1);
    const afterRisky = scorePlacement({ accuracy: 40, grade: "RISKY", isPerfect: false, floor: 5, levelScoreMultiplier: 1, combo }).combo;
    expect(afterRisky.multiplier).toBe(1);
    expect(afterRisky.perfectStreak).toBe(0);
  });

  it("height bonus increases score for higher floors, all else equal", () => {
    const low = scorePlacement({ accuracy: 90, grade: "GREAT", isPerfect: false, floor: 1, levelScoreMultiplier: 1, combo: initialComboState() });
    const high = scorePlacement({ accuracy: 90, grade: "GREAT", isPerfect: false, floor: 50, levelScoreMultiplier: 1, combo: initialComboState() });
    expect(high.gained).toBeGreaterThan(low.gained);
  });
});

describe("ScoreEngine", () => {
  it("accumulates score, perfect count, and average accuracy across placements", () => {
    const engine = new ScoreEngine();
    engine.place(100, 100, 1, true, 1); // perfect
    engine.place(80, 100, 2, false, 1); // 80% accuracy
    expect(engine.totalScore).toBeGreaterThan(0);
    expect(engine.perfectPlacementCount).toBe(1);
    expect(engine.averageAccuracy).toBeCloseTo(90, 0);
  });
});
