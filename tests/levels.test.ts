import { describe, expect, it } from "vitest";
import { getLevel, MAX_LEVEL, QUESTIONS_PER_LEVEL } from "@/quiz/levels";
import { DifficultyEngine } from "@/quiz/DifficultyEngine";

describe("getLevel", () => {
  it("has exactly MAX_LEVEL levels, each with QUESTIONS_PER_LEVEL questions", () => {
    for (let level = 1; level <= MAX_LEVEL; level++) {
      expect(getLevel(level).questionCount).toBe(QUESTIONS_PER_LEVEL);
    }
  });

  it("time limit strictly decreases as levels progress", () => {
    const times = Array.from({ length: MAX_LEVEL }, (_, i) => getLevel(i + 1).timeLimitMs);
    for (let i = 1; i < times.length; i++) {
      expect(times[i]).toBeLessThanOrEqual(times[i - 1]!);
    }
    expect(times[0]).toBeGreaterThan(times[times.length - 1]!);
  });

  it("difficulty range widens/rises as levels progress", () => {
    const level1 = getLevel(1);
    const level20 = getLevel(MAX_LEVEL);
    expect(level20.minDifficulty).toBeGreaterThanOrEqual(level1.minDifficulty);
    expect(level20.maxDifficulty).toBeGreaterThanOrEqual(level1.maxDifficulty);
  });

  it("clamps out-of-range levels instead of throwing", () => {
    expect(getLevel(0).level).toBe(1);
    expect(getLevel(999).level).toBe(MAX_LEVEL);
  });

  it("is deterministic (pure function of level)", () => {
    expect(getLevel(10)).toEqual(getLevel(10));
  });
});

describe("DifficultyEngine (Endless)", () => {
  it("keeps producing valid definitions well past level 20", () => {
    const engine = new DifficultyEngine();
    const round1 = engine.definitionForRound(1);
    const round50 = engine.definitionForRound(50);
    expect(round1.level).toBe(MAX_LEVEL + 1);
    expect(round50.level).toBe(MAX_LEVEL + 50);
    expect(round50.timeLimitMs).toBeGreaterThan(0);
  });

  it("never lets the time limit go below the configured floor", () => {
    const engine = new DifficultyEngine();
    const farRound = engine.definitionForRound(10000);
    expect(farRound.timeLimitMs).toBeGreaterThanOrEqual(3500);
  });
});
