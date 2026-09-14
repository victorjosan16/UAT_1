import { describe, expect, it } from "vitest";
import { revalidateRun } from "../src/lib/revalidate";
import { ScoreEngine } from "@/scoring/ScoreEngine";
import { buildLevelSequence } from "@/levels/levelSequence";
import type { PlacementResult, RunSummary } from "@/types";

/**
 * Builds a fully self-consistent RunSummary the same way the real client
 * would, by actually driving ScoreEngine + the seed's level sequence —
 * so these tests exercise the exact same math `revalidateRun` re-derives,
 * rather than hand-typing numbers that happen to look plausible.
 */
function buildValidSummary(seed: string, perfectPattern: boolean[]): RunSummary {
  const levelSequence = buildLevelSequence(seed, perfectPattern.length);
  const engine = new ScoreEngine();
  const placements: PlacementResult[] = [];

  let width = levelSequence[0]?.startingBlockWidth ?? 200;

  perfectPattern.forEach((isPerfect, i) => {
    const levelDef = levelSequence[i];
    if (!levelDef) throw new Error("level sequence too short");
    const overlapWidth = isPerfect ? width : width * 0.8;
    const floor = i + 1;
    const result = engine.place(overlapWidth, width, floor, isPerfect, levelDef.scoreMultiplier);

    placements.push({
      index: i,
      floor,
      accuracy: result.accuracy,
      grade: result.grade,
      isPerfect,
      perfectStreak: result.combo.perfectStreak,
      comboMultiplier: result.combo.multiplier,
      overlapWidth,
      blockWidthBefore: width,
      blockWidthAfter: overlapWidth,
      scoreGained: result.gained,
      totalScore: engine.totalScore,
      timestampMs: (i + 1) * 400,
    });

    width = overlapWidth;
  });

  return {
    mode: "CLASSIC",
    seed,
    gameVersion: "1.0.0",
    rulesVersion: 1,
    score: engine.totalScore,
    height: perfectPattern.length,
    perfectCount: perfectPattern.filter(Boolean).length,
    bestCombo: engine.comboState.bestCombo,
    bestPerfectStreak: engine.comboState.bestPerfectStreak,
    averageAccuracy: engine.averageAccuracy,
    placements,
    durationMs: perfectPattern.length * 400 + 500,
  };
}

describe("revalidateRun", () => {
  it("accepts a genuinely consistent run", () => {
    const summary = buildValidSummary("test-seed", [true, true, false, true, false, false, true, true]);
    expect(revalidateRun(summary)).toEqual({ valid: true });
  });

  it("rejects a tampered final score", () => {
    const summary = buildValidSummary("test-seed", [true, false, true, false]);
    summary.score += 5000;
    expect(revalidateRun(summary).valid).toBe(false);
  });

  it("rejects a tampered perfect count", () => {
    const summary = buildValidSummary("test-seed", [true, false, true, false]);
    summary.perfectCount = 99;
    expect(revalidateRun(summary).valid).toBe(false);
  });

  it("rejects height that doesn't match the number of placements", () => {
    const summary = buildValidSummary("test-seed", [true, false, true]);
    summary.height = 999;
    expect(revalidateRun(summary).valid).toBe(false);
  });

  it("rejects an overlap width larger than the block that was dropped", () => {
    const summary = buildValidSummary("test-seed", [false, false]);
    const first = summary.placements[0];
    if (first) first.overlapWidth = first.blockWidthBefore * 5;
    expect(revalidateRun(summary).valid).toBe(false);
  });

  // Note: seed *binding* (making sure a submitted run used the seed the
  // server actually issued for that session) is enforced by session.ts
  // comparing `session.seed !== summary.seed` before revalidateRun ever
  // runs — a short run's score can be identical across seeds since the
  // 20 classic levels are seed-independent (only Endless/DifficultyEngine
  // and modifier timing vary by seed). This test instead checks that
  // re-validation still holds once a run is long enough to cross into
  // seed-dependent Endless difficulty.
  it("accepts a long run that crosses from classic levels into seed-dependent Endless difficulty", () => {
    const summary = buildValidSummary(
      "endless-seed",
      Array.from({ length: 90 }, (_, i) => i % 3 === 0),
    );
    expect(revalidateRun(summary)).toEqual({ valid: true });
  });

  it("rejects an implausibly fast run (timestamps far too tight for the number of placements)", () => {
    const summary = buildValidSummary(
      "test-seed",
      Array.from({ length: 20 }, () => true),
    );
    summary.placements.forEach((p, i) => (p.timestampMs = i)); // 1ms per placement
    summary.durationMs = 20;
    expect(revalidateRun(summary).valid).toBe(false);
  });
});
