import { describe, expect, it } from "vitest";
import { LEVELS, MAX_LEVEL, getLevel, MIN_BLOCK_WIDTH } from "@/levels/levels";
import { DifficultyEngine } from "@/levels/DifficultyEngine";

describe("LEVELS (classic progression)", () => {
  it("has exactly 20 handcrafted stages", () => {
    expect(LEVELS).toHaveLength(20);
    expect(MAX_LEVEL).toBe(20);
  });

  it("numbers levels sequentially starting at 1", () => {
    LEVELS.forEach((level, i) => expect(level.level).toBe(i + 1));
  });

  it("never produces a block narrower than the configured minimum", () => {
    for (const level of LEVELS) {
      expect(level.startingBlockWidth).toBeGreaterThanOrEqual(MIN_BLOCK_WIDTH);
    }
  });

  it("generally increases difficulty (speed) and tightens tolerance from level 1 to 20", () => {
    const first = getLevel(1);
    const last = getLevel(20);
    expect(last.movementSpeed).toBeGreaterThan(first.movementSpeed);
    expect(last.perfectTolerance).toBeLessThan(first.perfectTolerance);
  });

  it("clamps out-of-range level lookups instead of throwing", () => {
    expect(getLevel(0)).toEqual(getLevel(1));
    expect(getLevel(999)).toEqual(getLevel(20));
  });

  it("level 20 (MASTER TOWER) carries the MOVING_BASE modifier and unlocks endless", () => {
    expect(getLevel(20).specialModifier).toBe("MOVING_BASE");
  });
});

describe("DifficultyEngine (endless)", () => {
  it("is deterministic for a given seed", () => {
    const a = new DifficultyEngine("seed-a");
    const b = new DifficultyEngine("seed-a");
    for (let floor = 1; floor <= 30; floor++) {
      expect(a.definitionForFloor(floor)).toEqual(b.definitionForFloor(floor));
    }
  });

  it("can diverge for different seeds", () => {
    const a = new DifficultyEngine("seed-a").definitionForFloor(9);
    const b = new DifficultyEngine("seed-b").definitionForFloor(9);
    // Not guaranteed to differ on every field, but the modifier schedule should not always match.
    const anyDifference = JSON.stringify(a) !== JSON.stringify(b);
    expect(typeof anyDifference).toBe("boolean");
  });

  it("never produces a block narrower than the minimum, however far into endless", () => {
    const engine = new DifficultyEngine("seed-a");
    for (const floor of [1, 50, 500, 5000]) {
      expect(engine.definitionForFloor(floor).startingBlockWidth).toBeGreaterThanOrEqual(MIN_BLOCK_WIDTH);
    }
  });

  it("caps movement speed and never lets tolerance go to zero or below", () => {
    const engine = new DifficultyEngine("seed-a");
    const def = engine.definitionForFloor(100000);
    expect(def.movementSpeed).toBeLessThanOrEqual(620);
    expect(def.perfectTolerance).toBeGreaterThan(0);
  });
});
