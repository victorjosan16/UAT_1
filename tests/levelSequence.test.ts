import { describe, expect, it } from "vitest";
import { buildLevelSequence } from "@/levels/levelSequence";
import { LEVELS, MAX_LEVEL } from "@/levels/levels";

describe("buildLevelSequence", () => {
  it("assigns level 1 to the first placement and matches LEVELS' requiredBlocks boundaries", () => {
    const total = LEVELS.reduce((sum, l) => sum + l.requiredBlocks, 0);
    const sequence = buildLevelSequence("seed", total);

    let index = 0;
    for (const level of LEVELS) {
      for (let i = 0; i < level.requiredBlocks; i++) {
        expect(sequence[index]?.level).toBe(level.level);
        index++;
      }
    }
  });

  it("continues past level 20 into Endless difficulty definitions", () => {
    const total = LEVELS.reduce((sum, l) => sum + l.requiredBlocks, 0);
    const sequence = buildLevelSequence("seed", total + 5);
    const lastFew = sequence.slice(total);
    for (const def of lastFew) {
      expect(def.level).toBeGreaterThan(MAX_LEVEL);
    }
  });

  it("is deterministic for the same seed", () => {
    const a = buildLevelSequence("same-seed", 50);
    const b = buildLevelSequence("same-seed", 50);
    expect(a).toEqual(b);
  });

  it("honors a custom startLevel (used for a direct Endless start)", () => {
    const sequence = buildLevelSequence("seed", 3, MAX_LEVEL + 1);
    expect(sequence[0]?.level).toBe(MAX_LEVEL + 1);
  });
});
